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
using ModuleProcessMonitor.Processes;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Infrastructure;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorer.Core.Infrastructure;
using ProcessExplorer.Core.Logging;
using ProcessExplorer.Core.Processes;

namespace ProcessExplorer.Core;

internal class ProcessInfoAggregator : IProcessInfoAggregator
{
    private readonly ILogger<IProcessInfoAggregator> _logger;
    private readonly ConcurrentDictionary<string, ProcessInfoCollectorData> _processInformation = new();
    private readonly ConcurrentDictionary<Guid, string> _subsysestemQueue = new(); //TODO(Lilla): put subsystem change messages to the queue and remove it if it has been sent
    private IProcessMonitor _processMonitor;
    private readonly ProcessInfoManager _processInfoManager;
    private ISubsystemController? _subsystemController;
    private readonly object _informationLocker = new();
    private bool _disposed;

    public ProcessInfoAggregator(
        ILogger<IProcessInfoAggregator>? logger,
        ProcessInfoManager processInfoManager,
        ISubsystemController? subsystemController = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;

        _processInfoManager = processInfoManager;
        _subsystemController = subsystemController;

        _processMonitor = new ProcessMonitor(_processInfoManager, _logger);
        SetUiCommunicatorsToWatchProcessChanges();
    }

    private static IEnumerable<IUIHandler> CreateCopyOfClients()
    {
        lock (UiClientsStore._uiClientLocker)
        {
            return UiClientsStore._uiClients.ToArray();
        }
    }

    public async Task AddInformation(string assemblyId, ProcessInfoCollectorData runtimeInfo)
    {
        lock (_informationLocker)
        {
            _processInformation.AddOrUpdate(assemblyId, runtimeInfo, (_, _) => runtimeInfo);
        }

        await UpdateInfoOnUI(handler => handler.AddRuntimeInfo(assemblyId, runtimeInfo));
    }

    public void RemoveRuntimeInformation(string assembly)
    {
        lock (_informationLocker)
        {
            _processInformation.TryRemove(assembly, out _);
        }
    }

    public void SetComposePid(int pid)
    {
        _processMonitor?.SetComposePid(pid);
    }

    private void SetUiCommunicatorsToWatchProcessChanges()
    {
        if (_processMonitor == null) return;

        _processMonitor.SetHandlers(
            ProcessModified,
            ProcessCreated,
            ProcessTerminated,
            ProcessesModified,
            ProcessStatusChanged);
    }

    private IEnumerable<ProcessInfoData> GetProcesses(ReadOnlySpan<int> ids)
    {
        if (_processMonitor == null) return Enumerable.Empty<ProcessInfoData>();

        if (_processInfoManager == null) return Enumerable.Empty<ProcessInfoData>();

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

        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.AddProcesses(processes);
            }
        }
    }

    private void ProcessTerminated(int pid)
    {
        _logger.ProcessTerminatedInformation(pid);

        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.TerminateProcess(pid);

                if (_processMonitor != null)
                {
                    var processes = GetProcesses(_processMonitor.GetProcessIds());
                    if (processes.Any()) client.AddProcesses(processes);
                }
            }
        }
    }

    private void ProcessCreated(int pid)
    {
        var process = GetProcess(pid);

        if (process.Equals(default)) return;

        _logger.ProcessCreatedInformation(pid);

        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.AddProcess((ProcessInfoData)process);
            }
        }
    }

    private ProcessInfoData? GetProcess(int pid)
    {
        if (_processMonitor == null) return null;

        var process = GetProcesses(_processMonitor.GetProcessIds()).FirstOrDefault(proc => proc.PID == pid);

        if (process.Equals(default)) return null;

        return process;
    }

    private void ProcessModified(int pid)
    {
        var process = GetProcess(pid);
        if (process.Equals(default)) return;

        _logger.ProcessModifiedDebug(pid);

        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.UpdateProcess((ProcessInfoData)process);
            }
        }
    }

    private void ProcessStatusChanged(KeyValuePair<int, Status> process)
    {
        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.UpdateProcessStatus(process);
            }
        }
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        _processMonitor?.SetDeadProcessRemovalDelay(delay);
    }

    public void AddUiConnection(IUIHandler uiHandler)
    {
        UiClientsStore.AddUiConnection(uiHandler);
        if (_processMonitor == null)
        {
            return;
        }

        ProcessesModified(_processMonitor.GetProcessIds());
        uiHandler.AddRuntimeInfo(_processInformation);
        _subsystemController?.SendInitializedSubsystemInfoToUis();

        _logger.ProcessMonitorCommunicatorIsSetDebug();
    }

    public void RemoveUiConnection(IUIHandler uiHandler)
    {
        UiClientsStore.RemoveUiConnection(uiHandler);
        _logger.UiCommunicatorIsRemovedDebug();
    }

    private ProcessInfoCollectorData? GetDataToModify(string assemblyId)
    {
        ProcessInfoCollectorData? data;

        lock (_informationLocker)
        {
            data = _processInformation
                .First(kvp => kvp.Key == assemblyId)
                .Value;
        }

        return data;
    }

    private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
    {
        lock (_informationLocker)
        {
            _processInformation.AddOrUpdate(assemblyId, data, (_, _) => data);
        }
    }

    public async Task AddConnectionCollection(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        var processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify == null) return;

        try
        {
            processInfoToModify.AddOrUpdateConnections(connections);
            UpdateProcessInfoCollectorData(assemblyId, processInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ConnectionCollectionCannotBeAddedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.AddConnections(assemblyId, connections));
    }

    public async Task UpdateConnectionInfo(string assemblyId, ConnectionInfo connectionInfo)
    {
        var processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify == null) return;

        try
        {
            processInfoToModify.UpdateConnection(connectionInfo);
            UpdateProcessInfoCollectorData(assemblyId, processInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ConnectionCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateConnection(assemblyId, connectionInfo));
    }

    public async Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        var processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify == null) return;

        try
        {
            processInfoToModify.UpdateEnvironmentVariables(environmentVariables);
            UpdateProcessInfoCollectorData(assemblyId, processInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.EnvironmentVariablesCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateEnvironmentVariables(assemblyId, environmentVariables));
    }

    public async Task UpdateRegistrationInfo(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        var processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify == null) return;

        try
        {
            processInfoToModify.UpdateRegistrations(registrations);
            UpdateProcessInfoCollectorData(assemblyId, processInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.RegistrationsCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateRegistrations(assemblyId, registrations));
    }

    public async Task UpdateModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        var processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify == null) return;

        try
        {
            processInfoToModify.UpdateModules(modules);
            UpdateProcessInfoCollectorData(assemblyId, processInfoToModify);
        }
        catch (Exception exception)
        {
            _logger.ModulesCannotBeUpdatedError(exception);
        }

        await UpdateInfoOnUI(handler => handler.UpdateModules(assemblyId, modules));
    }

    public async Task RemoveProcessById(int pid)
    {
        try
        {
            _processMonitor?.KillProcessById(pid);
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateProcessError(pid, exception);
        }

        await UpdateInfoOnUI(handler => handler.TerminateProcess(pid));
    }

    public void EnableWatchingSavedProcesses()
    {
        _processMonitor?.SetWatcher();
    }

    public void DisableWatchingProcesses()
    {
        _processMonitor?.Dispose();
    }

    public void InitProcesses(ReadOnlySpan<int> pids)
    {
        _processMonitor?.InitProcesses(pids);
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

    public Task SetProcessMonitor(IProcessMonitor processMonitor)
    {
        return new Task(() =>
        {
            UnSubscribeProcessMonitor();
            _processMonitor = processMonitor;
            SetUiCommunicatorsToWatchProcessChanges();
        });
    }

    private void UnSubscribeProcessMonitor()
    {

        _processMonitor.Dispose();
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

        UnSubscribeProcessMonitor();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void ScheduleSubsystemStateChanged(Guid instanceId, string state)
    {
        //_subsystemChangedMessage
    }
}
