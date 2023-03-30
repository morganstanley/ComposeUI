// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Collections.Concurrent;
using System.Diagnostics;
using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Logging;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Processes;

namespace ProcessExplorer.Core;

internal class ProcessInfoAggregator : IProcessInfoAggregator
{
    private readonly ILogger<IProcessInfoAggregator> _logger;
    private readonly ConcurrentDictionary<string, ProcessInfoCollectorData> _processInformation = new();
    private readonly object _processsInformationLock = new();
    //putting subsystem change messages to the queue and remove it if it has been sent ~ FIFO
    private readonly ConcurrentQueue<KeyValuePair<Guid, string>> _subsystemStateChanges = new();
    private readonly IProcessInfoManager _processInfoManager;
    private readonly ConcurrentDictionary<Guid, IUIHandler> _uiClients = new();
    private readonly object _uiClientLock = new();
    private bool _disposed;

    public ISubsystemController? SubsystemController { get; private set; }
    public int MainProcessId { get; private set; }
    public int TerminatingProcessDelay { get; private set; } = 1000;

    public ProcessInfoAggregator(
        ILogger<IProcessInfoAggregator>? logger,
        IProcessInfoManager processInfoManager,
        ISubsystemController? subsystemController = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;

        _processInfoManager = processInfoManager;
        SubsystemController = subsystemController;

        if (SubsystemController != null)
            SubsystemController.SetUiDelegate(UpdateInfoOnUI);

        SetUiCommunicatorsToWatchProcessChanges();
    }

    private IEnumerable<IUIHandler> CreateCopyOfClients()
    {
        lock (_uiClientLock)
        {
            return _uiClients.Select(kvp => kvp.Value);
        }
    }

    private void DisposeCore()
    {
        _processInfoManager.Dispose();
    }

    private Task UpdateInfoOnUI(Func<IUIHandler, Task> handlerAction)
    {
        try
        {
            return Task.WhenAll(CreateCopyOfClients().Select(handlerAction));
        }
        catch (Exception exception)
        {
            _logger.UiInformationCannotBeUpdatedError(exception);
            return Task.CompletedTask;
        }
    }

    private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
    {
        lock (_processsInformationLock)
        {
            _processInformation.AddOrUpdate(assemblyId, data, (_, _) => data);
        }
    }

    private void SetUiCommunicatorsToWatchProcessChanges()
    {
        _processInfoManager.SetHandlers(
            ProcessModified,
            ProcessTerminated,
            ProcessCreated,
            ProcessesModified,
            ProcessStatusChanged);
    }

    private IEnumerable<ProcessInfoData> GetProcesses(ReadOnlySpan<int> processIds)
    {
        var processes = new List<ProcessInfoData>();

        foreach (var id in processIds)
        {
            var process = ProcessInformation.GetProcessInfoWithCalculatedData(Process.GetProcessById(id), _processInfoManager);
            processes.Add(process.ProcessInfo);
        }

        return processes;
    }

    private void ProcessesModified(ReadOnlySpan<int> ids)
    {
        var processes = GetProcesses(ids);
        if (!processes.Any()) return;

        lock (_uiClientLock)
        {
            foreach (var client in _uiClients.Values)
            {
                client.AddProcesses(processes);
            }
        }
    }

    private async void ProcessTerminated(int processId)
    {
        _logger.ProcessTerminatedInformation(processId);

        await Task.Run(() =>
        {
            Task.Delay(TerminatingProcessDelay);
        });

        lock (_uiClientLock)
        {
            var processes = GetProcesses(_processInfoManager.GetProcessIds());

            foreach (var client in _uiClients.Values)
            {
                client.TerminateProcess(processId);
                if (processes.Any()) client.AddProcesses(processes);
            }
        }
    }

    private void ProcessCreated(int processId)
    {
        var process = GetProcess(processId);

        if (process == null) return;

        _logger.ProcessCreatedInformation(processId);

        lock (_uiClientLock)
        {
            foreach (var client in _uiClients.Values)
            {
                client.AddProcess((ProcessInfoData)process);
            }
        }
    }

    private ProcessInfoData? GetProcess(int processId)
    {
        var process = GetProcesses(_processInfoManager.GetProcessIds()).FirstOrDefault(proc => proc.ProcessId == processId);

        if (process == null) return null;

        return process;
    }

    private void ProcessModified(int processId)
    {
        var process = GetProcess(processId);
        if (process == null) return;

        _logger.ProcessModifiedDebug(processId);

        lock (_uiClientLock)
        {
            foreach (var client in _uiClients.Values)
            {
                client.UpdateProcess((ProcessInfoData)process);
            }
        }
    }

    private void ProcessStatusChanged(KeyValuePair<int, Status> process)
    {
        lock (_uiClientLock)
        {
            foreach (var client in _uiClients.Values)
            {
                client.UpdateProcessStatus(process);
            }
        }
    }

    public async Task RunSubsystemStateQueue(CancellationToken cancellationToken)
    {
        //TODO(Lilla): should i send here a warning if cancellationToken is default?
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_uiClients.IsEmpty || !_subsystemStateChanges.IsEmpty && SubsystemController != null)
            {
                var succeed = _subsystemStateChanges.TryDequeue(out var subsystemInfo);

                if (succeed && SubsystemController != null && SubsystemController != null) await SubsystemController.ModifySubsystemState(subsystemInfo.Key, subsystemInfo.Value);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // Perform any cleanup actions here
                break;
            }
        }
    }

    public async Task AddRuntimeInformation(string assemblyId, ProcessInfoCollectorData runtimeInfo)
    {
        lock (_processsInformationLock)
        {
            _processInformation.AddOrUpdate(assemblyId, runtimeInfo, (_, _) => runtimeInfo);
        }

        await UpdateInfoOnUI(handler => handler.AddRuntimeInfo(assemblyId, runtimeInfo));
    }

    public void RemoveRuntimeInformation(string assembly)
    {
        lock (_processsInformationLock)
        {
            _processInformation.TryRemove(assembly, out _);
        }
    }

    public void SetMainProcessId(int processId)
    {
        MainProcessId = processId;
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        TerminatingProcessDelay = delay * 100;
    }

    public void AddUiConnection(Guid id, IUIHandler uiHandler)
    {
        bool success;

        lock (_uiClientLock)
            success = _uiClients.TryAdd(id, uiHandler);

        if (!success) return;

        var processsIds = _processInfoManager.GetProcessIds();
        if (processsIds.Length > 0)
        {
            ProcessesModified(processsIds);
        }

        lock (_processsInformationLock)
        {
            if (_processInformation.Any())
                uiHandler.AddRuntimeInfo(_processInformation);
        }

        if (SubsystemController != null)
        {
            var subsystems = SubsystemController.GetSubsystems();
            if (subsystems.Any())
                uiHandler.AddSubsystems(subsystems);
        }

        _logger.ProcessMonitorCommunicatorIsSetDebug();
    }

    public void RemoveUiConnection(KeyValuePair<Guid, IUIHandler> handler)
    {
        bool success;
        lock (_uiClientLock)
        {
            success = _uiClients.TryRemove(handler);
        }

        if (!success) return;
        _logger.UiCommunicatorIsRemovedDebug();
    }

    private ProcessInfoCollectorData? GetRuntimeInformation(string assemblyId)
    {
        ProcessInfoCollectorData? data;

        lock (_processsInformationLock)
        {
            _processInformation.TryGetValue(assemblyId, out data);
        }

        return data;
    }

    public async Task AddConnectionCollection(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.AddOrUpdateConnections(connections);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ConnectionCollectionCannotBeAddedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.AddConnections(assemblyId, connections));
    }

    public async Task UpdateOrAddConnectionInfo(string assemblyId, ConnectionInfo connectionInfo)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateConnection(connectionInfo);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ConnectionCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateConnection(assemblyId, connectionInfo));
    }

    public async Task UpdateOrAddEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateOrAddEnvironmentVariables(environmentVariables);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.EnvironmentVariablesCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateEnvironmentVariables(assemblyId, environmentVariables));
    }

    public async Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateOrAddRegistrations(registrations);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.RegistrationsCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateRegistrations(assemblyId, registrations));
    }

    public async Task UpdateOrAddModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateOrAddModules(modules);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ModulesCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateModules(assemblyId, modules));
    }

    public void EnableWatchingSavedProcesses()
    {
        _processInfoManager.WatchProcesses(MainProcessId);
    }

    public void DisableWatchingProcesses()
    {
        _processInfoManager.Dispose();
    }

    public void InitProcesses(ReadOnlySpan<int> processIds)
    {
        _processInfoManager.ClearProcessIds();
        _processInfoManager.SetProcessIds(MainProcessId, processIds);
    }

    public void SetSubsystemController(ISubsystemController subsystemController)
    {
        SubsystemController = subsystemController;
        SubsystemController.SetUiDelegate(UpdateInfoOnUI);
    }

    public IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> GetRuntimeInformation()
    {
        lock (_processsInformationLock)
        {
            return _processInformation;
        }
    }

    public IEnumerable<KeyValuePair<Guid, IUIHandler>> GetUiClients()
    {
        lock (_uiClientLock)
        {
            return _uiClients;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        DisposeCore();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~ProcessInfoAggregator()
    {
        Dispose();
    }

    public void ScheduleSubsystemStateChanged(Guid instanceId, string state)
    {
        _subsystemStateChanges.Enqueue(new(instanceId, state));
    }

    public Task AddProcesses(ReadOnlySpan<int> processes)
    {
        _processInfoManager.SetProcessIds(MainProcessId, processes);
        return Task.CompletedTask;
    }
}
