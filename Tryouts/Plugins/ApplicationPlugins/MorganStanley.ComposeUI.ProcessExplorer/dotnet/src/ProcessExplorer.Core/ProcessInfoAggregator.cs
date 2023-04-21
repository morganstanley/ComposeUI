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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Entities;
using ProcessExplorer.Abstractions.Entities.Connections;
using ProcessExplorer.Abstractions.Entities.Modules;
using ProcessExplorer.Abstractions.Entities.Registrations;
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
    private readonly IProcessInfoMonitor _processInfoMonitor;
    private readonly IUiHandler _handler;
    private bool _disposed;

    public ISubsystemController? SubsystemController { get; private set; }
    public IUiHandler UiHandler { get; private set; }
    public int MainProcessId { get; private set; }
    public int TerminatingProcessDelay { get; private set; } = 1000;

    public ProcessInfoAggregator(
        IProcessInfoMonitor processInfoManager,
        IUiHandler handler,
        ISubsystemController? subsystemController = null,
        ILogger<IProcessInfoAggregator>? logger = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;

        _handler = handler;
        UiHandler = _handler;
        _processInfoMonitor = processInfoManager;
        SubsystemController = subsystemController;

        if (SubsystemController != null)
            SubsystemController.SetUiDelegate(UpdateInfoOnUI);

        SetUiCommunicatorsToWatchProcessChanges();
    }

    private void DisposeCore()
    {
        _processInfoMonitor.Dispose();
    }

    private Task UpdateInfoOnUI(Func<IUiHandler, Task> handlerAction)
    {
        try
        {
            return handlerAction.Invoke(_handler);
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
        _processInfoMonitor.SetHandlers(
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
            var process = ProcessInformation.GetProcessInfoWithCalculatedData(Process.GetProcessById(id), _processInfoMonitor);
            processes.Add(process.ProcessInfo);
        }

        return processes;
    }

    private void ProcessesModified(ReadOnlySpan<int> ids)
    {
        var processes = GetProcesses(ids);
        if (!processes.Any()) return;
        _handler.AddProcesses(processes);
    }

    private async void ProcessTerminated(int processId)
    {
        _logger.ProcessTerminatedInformation(processId);

        await Task.Run(() =>
        {
            Task.Delay(TerminatingProcessDelay);
        });

        var processes = GetProcesses(_processInfoMonitor.GetProcessIds());
        
        await _handler.TerminateProcess(processId);
        await _handler.AddProcesses(processes);
    }

    private async void ProcessCreated(int processId)
    {
        var process = GetProcess(processId);

        if (process == null) return;

        _logger.ProcessCreatedInformation(processId);

        await _handler.AddProcess(process);
    }

    private ProcessInfoData? GetProcess(int processId)
    {
        var process = GetProcesses(_processInfoMonitor.GetProcessIds()).FirstOrDefault(proc => proc.ProcessId == processId);

        if (process == null) return null;

        return process;
    }

    private async void ProcessModified(int processId)
    {
        var process = GetProcess(processId);
        if (process == null) return;

        _logger.ProcessModifiedDebug(processId);

        await _handler.UpdateProcess(process);
    }

    private async void ProcessStatusChanged(KeyValuePair<int, Status> process)
    {
        await _handler.UpdateProcessStatus(process);
    }

    public async Task RunSubsystemStateQueue(CancellationToken cancellationToken)
    {
        //TODO(Lilla): should i send here a warning if cancellationToken is default?
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_subsystemStateChanges.IsEmpty && SubsystemController != null)
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

    public IEnumerable<ProcessInfoData> GetProcesses()
    {
        var processIds = _processInfoMonitor.GetProcessIds();
        return GetProcesses(processIds);
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
        _processInfoMonitor.WatchProcesses(MainProcessId);
    }

    public void DisableWatchingProcesses()
    {
        _processInfoMonitor.Dispose();
    }

    public void InitProcesses(ReadOnlySpan<int> processIds)
    {
        _processInfoMonitor.ClearProcessIds();
        _processInfoMonitor.SetProcessIds(MainProcessId, processIds);
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
        _processInfoMonitor.SetProcessIds(MainProcessId, processes);
        return Task.CompletedTask;
    }
}
