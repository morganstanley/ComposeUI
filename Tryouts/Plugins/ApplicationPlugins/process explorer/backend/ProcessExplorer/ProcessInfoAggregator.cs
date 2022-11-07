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
using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Processes;
using ProcessExplorer.Infrastructure;
using ProcessExplorer.Logging;

namespace ProcessExplorer;

internal class ProcessInfoAggregator : IProcessInfoAggregator
{
    private readonly ILogger<IProcessInfoAggregator> _logger;
    public ConcurrentDictionary<string, ProcessInfoCollectorData> ProcessInformation { get; } = new();
    public IProcessMonitor? ProcessMonitor { get; private set; }
    private readonly object _informationLocker = new();

    public ProcessInfoAggregator(ILogger<IProcessInfoAggregator>? logger, IProcessMonitor? processMonitor)
    {
        _logger = logger ?? NullLogger<IProcessInfoAggregator>.Instance;

        if (processMonitor == null)
        {
            return;
        }

        ProcessMonitor = processMonitor;
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
            ProcessInformation.AddOrUpdate(assemblyId, runtimeInfo, (_, _) => runtimeInfo);
        }
        await UpdateInfoOnUI(handler => handler.AddRuntimeInfo(assemblyId, runtimeInfo));
    }

    public void RemoveAInfoAggregatorInformation(string assembly)
    {
        lock (_informationLocker)
        {
            ProcessInformation.TryRemove(assembly, out _);
        }
    }

    public void SetComposePid(int pid)
    {
        ProcessMonitor?.SetComposePid(pid);
    }

    public IEnumerable<ProcessInfoData>? GetProcesses()
    {
        return ProcessMonitor?.GetProcesses();
    }


    private void SetUiCommunicatorsToWatchProcessChanges()
    {
        if (ProcessMonitor == null)
        {
            return;
        }

        ProcessMonitor._processCreated += ProcessCreated;
        ProcessMonitor._processModified += ProcessModified;
        ProcessMonitor._processTerminated += ProcessTerminated;
        ProcessMonitor._processesModified += ProcessesModified;
    }

    private void ProcessesModified(object? sender, SynchronizedCollection<ProcessInfoData> e)
    {
        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.AddProcesses(e);
            }
        }
    }

    private void ProcessTerminated(object? sender, int e)
    {
        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.RemoveProcessByID(e);
                if (ProcessMonitor != null)
                {
                    client.AddProcesses(ProcessMonitor.GetProcesses()!);
                }
            }
        }
    }

    private void ProcessModified(object? sender, ProcessInfoData e)
    {
        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.UpdateProcess(e);
            }
        }
    }

    private void ProcessCreated(object? sender, ProcessInfoData e)
    {
        lock (UiClientsStore._uiClientLocker)
        {
            foreach (var client in UiClientsStore._uiClients)
            {
                client.AddProcess(e);
            }
        }
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        ProcessMonitor?.SetDeadProcessRemovalDelay(delay);
    }

    public void AddUiConnection(IUIHandler uiHandler)
    {
        UiClientsStore.AddUiConnection(uiHandler);
        if (ProcessMonitor == null)
        {
            return;
        }

        uiHandler.AddProcesses(ProcessMonitor.GetProcesses()!);
        uiHandler.AddRuntimeInfo(ProcessInformation);
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
            data = ProcessInformation
                .First(kvp => kvp.Key == assemblyId)
                .Value;
        }

        return data;
    }

    private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
    {
        lock (_informationLocker)
        {
            ProcessInformation.AddOrUpdate(assemblyId, data, (_, _) => data);
        }
    }

    public async Task AddConnectionCollection(string assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        ProcessInfoCollectorData? processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify != null)
        {
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
    }

    public async Task UpdateConnectionInfo(string assemblyId, ConnectionInfo connectionInfo)
    {
        ProcessInfoCollectorData? processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify != null)
        {
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
    }

    public async Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        ProcessInfoCollectorData? processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify != null)
        {
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
    }

    public async Task UpdateRegistrationInfo(string assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        ProcessInfoCollectorData? processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify != null)
        {
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
    }

    public async Task UpdateModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules)
    {
        ProcessInfoCollectorData? processInfoToModify = GetDataToModify(assemblyId);

        if (processInfoToModify != null)
        {
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
    }

    public async Task RemoveProcessById(int pid)
    {
        try
        {
            ProcessMonitor?.KillProcessById(pid);
        }
        catch (Exception exception)
        {
            _logger.CannotTerminateProcessError(pid, exception);
        }
        await UpdateInfoOnUI(handler => handler.RemoveProcessByID(pid));
    }

    public void EnableWatchingSavedProcesses()
    {
        ProcessMonitor?.SetWatcher();
    }

    public void DisableWatchingProcesses()
    {
        ProcessMonitor?.UnsetWatcher();
    }

    public void InitProcesses(IEnumerable<ProcessInfoData> processInfo)
    {
        ProcessMonitor?.InitProcesses(processInfo);
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
            ProcessMonitor = processMonitor;
            SetUiCommunicatorsToWatchProcessChanges();
        });
    }

    private void UnSubscribeProcessMonitor()
    {
        if (ProcessMonitor == null)
        {
            return;
        }

        ProcessMonitor._processCreated -= ProcessCreated;
        ProcessMonitor._processModified -= ProcessModified;
        ProcessMonitor._processTerminated -= ProcessTerminated;
        ProcessMonitor._processesModified -= ProcessesModified;

        ProcessMonitor = null;
    }

    public void AddProcessInfo(ProcessInfoData processInfo)
    {
        ProcessMonitor?.AddProcessInfo(processInfo);
    }
}
