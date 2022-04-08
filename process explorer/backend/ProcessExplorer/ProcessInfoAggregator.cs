/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;
using ProcessExplorer.LocalCollector;
using ProcessExplorer.Processes.Communicator;
using ProcessExplorer.Processes.Logging;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Registrations;
using ProcessExplorer.LocalCollector.Modules;

namespace ProcessExplorer
{
    public class ProcessInfoAggregator : IProcessInfoAggregator
    {
        private readonly ILogger<ProcessInfoAggregator>? logger;

        public ConcurrentDictionary<string, ProcessInfoCollectorData>? Information { get; } =
            new ConcurrentDictionary<string, ProcessInfoCollectorData>();

        public IProcessMonitor? ProcessMonitor { get; }
        private SynchronizedCollection<IUIHandler> UIClients = new SynchronizedCollection<IUIHandler>();
        private readonly object informationLocker = new object();
        private readonly object uiClientLocker = new object();

        public ProcessInfoAggregator(ILogger<ProcessInfoAggregator> logger, IProcessMonitor processMonitor)
        {
            this.logger = logger;
            ProcessMonitor = processMonitor;
            SetUICommunicatorsToWatchProcessChanges();
        }

        private SynchronizedCollection<IUIHandler> CreateCopyOfClients()
        {
            SynchronizedCollection<IUIHandler> UIHandlersCopy;
            lock (uiClientLocker)
            {
                UIHandlersCopy = UIClients;
            }

            return UIHandlersCopy;
        }

        public async Task AddInformation(string assemblyId, ProcessInfoCollectorData processInfo)
        {
            lock (informationLocker)
                Information?.AddOrUpdate(assemblyId, processInfo, (_, _) => processInfo);

            SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();

            foreach (var uiClient in UIHandlersCopy)
            {
                await uiClient.AddRuntimeInfo(processInfo);
                if(Information is not null)
                    await uiClient.AddRuntimeInfos(Information.Values);
            }
        }

        public void RemoveAInfoAggregatorInformation(string assembly)
        {
            lock (informationLocker)
                Information?.TryRemove(assembly, out _);
        }

        public void SetComposePID(int pid)
            => ProcessMonitor?.SetComposePID(pid);

        public SynchronizedCollection<ProcessInfoData>? RefreshProcessList()
            => ProcessMonitor?.GetProcesses();

        public SynchronizedCollection<ProcessInfoData>? GetProcesses()
            => ProcessMonitor?.GetProcesses();

        public void InitProcessExplorer()
            => ProcessMonitor?.FillListWithRelatedProcesses();

        public void SetWatcher()
            => ProcessMonitor?.SetWatcher();

        private void SetUICommunicatorsToWatchProcessChanges()
        {
            if (ProcessMonitor is not null)
            {
                ProcessMonitor.processCreatedAction += ProcessCreated;
                ProcessMonitor.processModifiedAction += ProcessModified;
                ProcessMonitor.processTerminatedAction += ProcessTerminated;
            }
        }

        private void ProcessTerminated(object? sender, int e)
        {
            lock (uiClientLocker)
            {
                foreach (var client in UIClients)
                {
                    client.RemoveProcess(e);
                    client.AddProcesses(ProcessMonitor?.Data.Processes);
                }
            }
        }

        private void ProcessModified(object? sender, ProcessInfoData e)
        {
            lock (uiClientLocker)
            {
                foreach (var client in UIClients)
                {
                    client.UpdateProcess(e);
                }
            }
        }

        private void ProcessCreated(object? sender, ProcessInfoData e)
        {
            lock (uiClientLocker)
            {
                foreach (var client in UIClients)
                {
                    client.AddProcess(e);
                }
            }
        }

        public void SetDeadProcessRemovalDelay(int delay)
            => ProcessMonitor?.SetDeadProcessRemovalDelay(delay);

        public void AddUIConnection(IUIHandler uiHandler)
        {
            lock (uiClientLocker)
            {
                UIClients.Add(uiHandler);
            }

            uiHandler.AddProcesses(ProcessMonitor?.Data.Processes);

            logger?.ProcessCommunicatorIsSet();
        }

        private ProcessInfoCollectorData? GetDataToModify(string assemblyId)
        {
            ProcessInfoCollectorData? data = null;
            lock (informationLocker)
            {
                if (Information is not null)
                    data = Information.FirstOrDefault(kvp => kvp.Key == assemblyId).Value;
            }

            return data;
        }

        private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
        {
            lock (informationLocker)
            {
                if (Information is not null)
                    Information.AddOrUpdate(assemblyId, data, (_, _) => data);
            }
        }

        public async Task AddConnectionCollection(string assemblyId, SynchronizedCollection<ConnectionInfo> connections)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    if (Information is not null)
                    {
                        data.AddOrUpdateConnections(connections);
                        UpdateProcessInfoCollectorData(assemblyId, data);
                    }
                }
                catch (Exception exception)
                {
                    logger?.ConnectionCollectionCannotBeAdded(exception);
                }

                SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
                foreach (var uiClient in UIHandlersCopy)
                {
                    await uiClient.AddConnections(connections);
                }
            }
        }

        public async Task UpdateConnectionInfo(string assemblyId, ConnectionInfo connectionInfo)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateConnection(connectionInfo);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.ConnectionCannotBeUpdated(exception);
                }

                SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
                foreach (var uiClient in UIHandlersCopy)
                {
                    await uiClient.UpdateConnection(connectionInfo);
                }
            }
        }

        public async Task UpdateEnvironmentVariablesInfo(string assemblyId, EnvironmentMonitorInfo environmentVariables)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateEnvironmentVariables(environmentVariables.EnvironmentVariables);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.EnvironmentVariablesCannotBeUpdated(exception);
                }

                SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
                foreach (var uiClient in UIHandlersCopy)
                {
                    await uiClient.UpdateEnvironmentVariables(environmentVariables.EnvironmentVariables);
                }
            }
        }

        public async Task UpdateRegistrationInfo(string assemblyId, RegistrationMonitorInfo registrations)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateRegistrations(registrations.Services);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.RegistrationsCannotBeUpdated(exception);
                }

                SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
                foreach (var uiClient in UIHandlersCopy)
                {
                    await uiClient.UpdateRegistrations(registrations.Services);
                }
            }
        }

        public async Task UpdateModuleInfo(string assemblyId, ModuleMonitorInfo modules)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateModules(modules.CurrentModules);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.ModulesCannotBeUpdated(exception);
                }

                SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
                foreach (var uiClient in UIHandlersCopy)
                {
                    await uiClient.UpdateModules(modules.CurrentModules);
                }
            }
        }
    }
}
