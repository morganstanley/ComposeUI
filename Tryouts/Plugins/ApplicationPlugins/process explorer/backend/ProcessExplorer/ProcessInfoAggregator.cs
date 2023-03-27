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
    private readonly ProcessInfoManager _processInfoManager;
    private ISubsystemController? _subsystemController;
    private readonly ConcurrentDictionary<Guid, IUIHandler> _uiClients = new();
    private readonly object _uiClientLock = new();
    private bool _disposed;

    public ProcessInfoAggregator(
        ILogger<IProcessInfoAggregator>? logger,
        ProcessInfoManager processInfoManager,
        ISubsystemController? subsystemController = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;

        _processInfoManager = processInfoManager;
        _subsystemController = subsystemController;

        if (_subsystemController != null)
            _subsystemController.SetUiDelegate(UpdateInfoOnUI);

        SetUiCommunicatorsToWatchProcessChanges();
    }

    public async Task RunSubsystemStateQueue(CancellationToken cancellationToken)
    {
        //TODO(Lilla): should i send here a warning if cancellationToken is default?
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_uiClients.IsEmpty || !_subsystemStateChanges.IsEmpty && _subsystemController != null)
            {
                var succeed = _subsystemStateChanges.TryDequeue(out var subsystemInfo);

                if (succeed) await ModifySubsystemState(subsystemInfo.Key, subsystemInfo.Value);
            }
        }
    }

    private IEnumerable<IUIHandler> CreateCopyOfClients()
    {
        lock (_uiClientLock)
        {
            return _uiClients.Select(kvp => kvp.Value);
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

    public void SetComposePid(int pid)
    {
        _processInfoManager?.SetComposePid(pid);
    }

    private void SetUiCommunicatorsToWatchProcessChanges()
    {
        _processInfoManager.SetHandlers(
            ProcessModified,
            ProcessCreated,
            ProcessTerminated,
            ProcessesModified,
            ProcessStatusChanged);
    }

    private IEnumerable<ProcessInfoData> GetProcesses(ReadOnlySpan<int> ids)
    {
        var processes = new List<ProcessInfoData>();

        foreach (var id in ids)
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

    private void ProcessTerminated(int pid)
    {
        _logger.ProcessTerminatedInformation(pid);

        lock (_uiClientLock)
        {
            var processes = GetProcesses(_processInfoManager.GetProcessIds());

            foreach (var client in _uiClients.Values)
            {
                client.TerminateProcess(pid);
                if (processes.Any()) client.AddProcesses(processes);
            }
        }
    }

    private void ProcessCreated(int pid)
    {
        var process = GetProcess(pid);

        if (process.Equals(default)) return;

        _logger.ProcessCreatedInformation(pid);

        lock (_uiClientLock)
        {
            foreach (var client in _uiClients.Values)
            {
                client.AddProcess((ProcessInfoData)process);
            }
        }
    }

    private ProcessInfoData? GetProcess(int pid)
    {
        var process = GetProcesses(_processInfoManager.GetProcessIds()).FirstOrDefault(proc => proc.PID == pid);

        if (process.Equals(default)) return null;

        return process;
    }

    private void ProcessModified(int pid)
    {
        var process = GetProcess(pid);
        if (process.Equals(default)) return;

        _logger.ProcessModifiedDebug(pid);

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

    public void SetDeadProcessRemovalDelay(int delay)
    {
        _processInfoManager?.SetDeadProcessRemovalDelay(delay);
    }

    public void AddUiConnection(Guid id, IUIHandler uiHandler)
    {
        bool success;

        lock (_uiClientLock)
            success = _uiClients.TryAdd(id, uiHandler);

        if (!success) return;

        ProcessesModified(_processInfoManager.GetProcessIds());
        uiHandler.AddRuntimeInfo(_processInformation);

        if (_subsystemController != null)
            uiHandler.AddSubsystems(_subsystemController.GetSubsystems());

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

    private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
    {
        lock (_processsInformationLock)
        {
            _processInformation.AddOrUpdate(assemblyId, data, (_, _) => data);
        }
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

    public async Task UpdateConnectionInfo(string assemblyId, ConnectionInfo connectionInfo)
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

    public async Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateEnvironmentVariables(environmentVariables);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.EnvironmentVariablesCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateEnvironmentVariables(assemblyId, environmentVariables));
    }

    public async Task UpdateRegistrationInfo(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateRegistrations(registrations);
            UpdateProcessInfoCollectorData(assemblyId, runtimeInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.RegistrationsCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateRegistrations(assemblyId, registrations));
    }

    public async Task UpdateModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        var runtimeInfoToModify = GetRuntimeInformation(assemblyId);

        if (runtimeInfoToModify == null) return;

        try
        {
            runtimeInfoToModify.UpdateModules(modules);
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
        _processInfoManager.WatchProcesses();
    }

    public void DisableWatchingProcesses()
    {
        _processInfoManager.Dispose();
    }

    public void InitProcesses(ReadOnlySpan<int> pids)
    {
        _processInfoManager.ClearProcessIds();
        _processInfoManager.SetProcessIds(pids);
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

    private void DisposeCore()
    {
        _processInfoManager.Dispose();
    }

    public Task ShutdownSubsystems(IEnumerable<string> subsystemIds)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.ShutdownSubsystems(subsystemIds);
    }

    public Task RestartSubsystems(IEnumerable<string> subsystemIds)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.RestartSubsystems(subsystemIds);
    }

    public Task LaunchSubsystems(IEnumerable<string> subsystemIds)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.LaunchSubsystems(subsystemIds);
    }

    public Task LaunchSubsystemWithDelay(Guid id, int periodOfTime)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.LaunchSubsystemAfterTime(id, periodOfTime);
    }

    public Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.InitializeSubsystems(subsystems);
    }

    public Task ModifySubsystemState(Guid subsystemId, string state)
    {
        if (_subsystemController == null) return Task.CompletedTask;
        return _subsystemController.ModifySubsystemState(subsystemId, state);
    }

    public void SetSubsystemController(ISubsystemController subsystemController)
    {
        _subsystemController = subsystemController;
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

    public ValueTask AddProcesses(ReadOnlySpan<int> processes)
    {
        _processInfoManager.SetProcessIds(processes);
        return ValueTask.CompletedTask;
    }
}
