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
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Connections;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Modules;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Registrations;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Logging;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Processes;

namespace MorganStanley.ComposeUI.ProcessExplorer.Core;

internal class ProcessInfoAggregator : IProcessInfoAggregator
{
    private readonly ILogger<IProcessInfoAggregator> _logger;
    private readonly IProcessInfoMonitor _processInfoMonitor;
    private readonly IUiHandler _handler;
    private readonly ISubsystemController _subsystemController;

    private readonly ConcurrentDictionary<string, ProcessInfoCollectorData> _processInformation = new();
    //putting subsystem change messages to the queue and remove it if it has been sent ~ FIFO
    private readonly ConcurrentQueue<KeyValuePair<Guid, string>> _subsystemStateChanges = new();

    public ISubsystemController SubsystemController => _subsystemController;
    public IUiHandler UiHandler => _handler;
    public int TerminatingProcessDelay { get; private set; } = 1000;
    public int MainProcessId { get; set; }

    public ProcessInfoAggregator(
        IProcessInfoMonitor processInfoMonitor,
        IUiHandler handler,
        ISubsystemController subsystemController,
        ILogger<IProcessInfoAggregator>? logger = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;
        _handler = handler;
        _processInfoMonitor = processInfoMonitor;
        _subsystemController = subsystemController;

        _processInfoMonitor.ProcessIds
            .Select(kvp => Observable.FromAsync(async () => await PushNotification(kvp)))
            .Concat()
            .Subscribe();
    }

    private async Task PushNotification(KeyValuePair<int, ProcessStatus> kvp)
    {
        switch (kvp.Value)
        {
            case ProcessStatus.Running:
                await ProcessCreated(kvp.Key);
                break;

            case ProcessStatus.Terminated:
            case ProcessStatus.Stopped:
                await ProcessTerminated(kvp.Key);
                break;

            case ProcessStatus.Modified:
                await ProcessModified(kvp.Key);
                break;

            default:
                return;
        }
    }

    private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
    {
        _processInformation.AddOrUpdate(assemblyId, data, (_, _) => data);
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

    private async Task ProcessTerminated(int processId)
    {
        _logger.ProcessTerminatedInformation(processId);
        await ProcessStatusChanged(new(processId, ProcessStatus.Terminated));

        await Task.Run(() =>
        {
            Task.Delay(TerminatingProcessDelay);
        });

        var processes = GetProcesses(_processInfoMonitor.GetProcessIds());

        await _handler.TerminateProcess(processId);
        await _handler.AddProcesses(processes);
    }

    private async Task ProcessCreated(int processId)
    {
        var process = GetProcess(processId);

        if (process == null) return;

        _logger.ProcessCreatedInformation(processId);

        await _handler.AddProcess(process);
    }

    private ProcessInfoData? GetProcess(int processId)
    {
        var process = GetProcesses(_processInfoMonitor.GetProcessIds()).FirstOrDefault(proc => proc.ProcessId == processId);

        return process ?? null;
    }

    private async Task ProcessModified(int processId)
    {
        var process = GetProcess(processId);
        if (process == null) return;

        _logger.ProcessModifiedDebug(processId);

        await _handler.UpdateProcess(process);
    }

    private async Task ProcessStatusChanged(KeyValuePair<int, ProcessStatus> process)
    {
        await _handler.UpdateProcessStatus(process);
    }

    public async Task RunSubsystemStateQueue(CancellationToken cancellationToken)
    {
        //TODO(Lilla): should i send here a warning if cancellationToken is default?
        while (!cancellationToken.IsCancellationRequested)
        {
            var succeed = _subsystemStateChanges.TryDequeue(out var subsystemInfo);
            if (succeed) await SubsystemController.ModifySubsystemState(subsystemInfo.Key, subsystemInfo.Value);
        }
    }

    public IEnumerable<ProcessInfoData> GetProcesses()
    {
        var processIds = _processInfoMonitor.GetProcessIds();
        return GetProcesses(processIds);
    }

    public async Task AddRuntimeInformation(string assemblyId, ProcessInfoCollectorData runtimeInfo)
    {
        _processInformation.AddOrUpdate(assemblyId, runtimeInfo, (_, _) => runtimeInfo);

        await _handler.AddRuntimeInfo(assemblyId, runtimeInfo);
    }

    public void RemoveRuntimeInformation(string assembly)
    {
        if (!_processInformation.TryRemove(assembly, out _)) _logger.UnableToRemoveRuntimeInformationError(assembly);
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        TerminatingProcessDelay = delay * 100;
    }

    private ProcessInfoCollectorData? GetRuntimeInformation(string assemblyId)
    {
        _processInformation.TryGetValue(assemblyId, out var data);

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

        await _handler.AddConnections(assemblyId, connections);
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

        await _handler.UpdateConnection(assemblyId, connectionInfo);
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

        await _handler.UpdateEnvironmentVariables(assemblyId, environmentVariables);
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

        await _handler.UpdateRegistrations(assemblyId, registrations);
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

        await _handler.UpdateModules(assemblyId, modules);
    }

    public void EnableWatchingSavedProcesses()
    {
        _processInfoMonitor.WatchProcesses(MainProcessId);
    }

    public void DisableWatchingProcesses()
    {
        _processInfoMonitor.StopWatchingProcesses();
    }

    public void InitProcesses(ReadOnlySpan<int> processIds)
    {
        _processInfoMonitor.ClearProcessIds();
        _processInfoMonitor.SetProcessIds(MainProcessId, processIds);
    }

    public IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> GetRuntimeInformation()
    {
        return _processInformation;
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
