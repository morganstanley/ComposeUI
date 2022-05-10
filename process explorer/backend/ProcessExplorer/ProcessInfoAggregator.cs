/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;
using ProcessExplorer.LocalCollector;
using ProcessExplorer.Processes.Communicator;
using ProcessExplorer.Processes.Logging;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.Registrations;
using ProcessExplorer.LocalCollector.Modules;

namespace ProcessExplorer
{
    public class ProcessInfoAggregator : IProcessInfoAggregator
    {
        private readonly ILogger<ProcessInfoAggregator>? logger;

        public ConcurrentDictionary<string, ProcessInfoCollectorData> Information { get; } =
            new ConcurrentDictionary<string, ProcessInfoCollectorData>();

        public IProcessMonitor ProcessMonitor { get; }
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
            {
                Information?.AddOrUpdate(assemblyId, processInfo, (_, _) => processInfo);
            }
            await UpdateInfoOnUI(processInfo);
        }

        public void RemoveAInfoAggregatorInformation(string assembly)
        {
            lock (informationLocker)
                Information.TryRemove(assembly, out _);
        }

        public void SetComposePID(int pid)
            => ProcessMonitor.SetComposePID(pid);

        public IEnumerable<ProcessInfoData>? RefreshProcessList()
            => ProcessMonitor.GetProcesses();

        public IEnumerable<ProcessInfoData>? GetProcesses()
            => ProcessMonitor.GetProcesses();

        public void InitProcessExplorer()
            => ProcessMonitor.FillListWithRelatedProcesses();

        public void SetWatcher()
            => ProcessMonitor.SetWatcher();

        private void SetUICommunicatorsToWatchProcessChanges()
        {

            ProcessMonitor.processCreatedAction += ProcessCreated;
            ProcessMonitor.processModifiedAction += ProcessModified;
            ProcessMonitor.processTerminatedAction += ProcessTerminated;
            ProcessMonitor.processesModifiedAction += ProcessesModified;

        }

        private void ProcessesModified(object? sender, SynchronizedCollection<ProcessInfoData> e)
        {
            lock (uiClientLocker)
            {
                foreach (var client in UIClients)
                {
                    client.AddProcesses(e);
                }
            }
        }

        private void ProcessTerminated(object? sender, int e)
        {
            lock (uiClientLocker)
            {
                foreach (var client in UIClients)
                {
                    client.RemoveProcess(e);
                    client.AddProcesses(ProcessMonitor.Data.Processes);
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

            uiHandler.AddProcesses(ProcessMonitor.Data.Processes);

            logger?.ProcessCommunicatorIsSetDebug();
        }

        public void RemoveUIConnection(IUIHandler UIHandler)
        {
            lock (uiClientLocker)
            {
                UIClients.Remove(UIHandler);
            }
            logger?.UICommunicatorIsRemovedDebug();
        }

        private ProcessInfoCollectorData? GetDataToModify(string assemblyId)
        {
            ProcessInfoCollectorData? data = null;
            lock (informationLocker)
            {
                data = Information.FirstOrDefault(kvp => kvp.Key == assemblyId).Value;
            }

            return data;
        }

        private void UpdateProcessInfoCollectorData(string assemblyId, ProcessInfoCollectorData data)
        {
            lock (informationLocker)
            {
                Information.AddOrUpdate(assemblyId, data, (_, _) => data);
            }
        }

        public async Task AddConnectionCollection(string assemblyId, IEnumerable<ConnectionInfo> connections)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.AddOrUpdateConnections(connections);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }

                catch (Exception exception)
                {
                    logger?.ConnectionCollectionCannotBeAddedError(exception);
                }
                await UpdateInfoOnUI(connections);
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
                    logger?.ConnectionCannotBeUpdatedError(exception);
                }
                await UpdateInfoOnUI(connectionInfo);
            }
        }

        public async Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateEnvironmentVariables(environmentVariables);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.EnvironmentVariablesCannotBeUpdatedError(exception);
                }
                await UpdateInfoOnUI(environmentVariables);
            }
        }

        public async Task UpdateRegistrationInfo(string assemblyId, IEnumerable<RegistrationInfo> registrations)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateRegistrations(registrations);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.RegistrationsCannotBeUpdatedError(exception);
                }
                await UpdateInfoOnUI(registrations);
            }
        }

        public async Task UpdateModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules)
        {
            ProcessInfoCollectorData? data = GetDataToModify(assemblyId);
            if (data is not null)
            {
                try
                {
                    data.UpdateModules(modules);
                    UpdateProcessInfoCollectorData(assemblyId, data);
                }
                catch (Exception exception)
                {
                    logger?.ModulesCannotBeUpdatedError(exception);
                }
                await UpdateInfoOnUI(modules);
            }
        }

        private async Task NotifyUI(IUIHandler handler, dynamic collectionOrObjectToTransform)
        {
            if (CastTo<ProcessInfoCollectorData>(collectionOrObjectToTransform) is not null)
            {
                await handler.AddRuntimeInfo(CastTo<ProcessInfoCollectorData>(collectionOrObjectToTransform));
            }
            else if (CastTo<IEnumerable<ModuleInfo>>(collectionOrObjectToTransform) is not null)
            {
                await handler.UpdateModules(CastTo<IEnumerable<ModuleInfo>>(collectionOrObjectToTransform));
            }
            else if (CastTo<IEnumerable<RegistrationInfo>>(collectionOrObjectToTransform) is not null)
            {
                await handler.UpdateRegistrations(CastTo<IEnumerable<RegistrationInfo>>(collectionOrObjectToTransform));
            }
            else if (CastTo<IEnumerable<ConnectionInfo>>(collectionOrObjectToTransform) is not null)
            {
                await handler.AddConnections(CastTo<IEnumerable<ConnectionInfo>>(collectionOrObjectToTransform));
            }
            else if (CastTo<ConnectionInfo>(collectionOrObjectToTransform) is not null)
            {
                await handler.UpdateConnection(CastTo<ConnectionInfo>(collectionOrObjectToTransform));
            }
            else if (CastTo<IEnumerable<KeyValuePair<string, string>>>(collectionOrObjectToTransform) is not null)
            {
                await handler.UpdateEnvironmentVariables(CastTo<IEnumerable<KeyValuePair<string, string>>>(collectionOrObjectToTransform));
            }
        }

        private async Task UpdateInfoOnUI(object changedElement)
        {
            SynchronizedCollection<IUIHandler> UIHandlersCopy = CreateCopyOfClients();
            foreach (var uiClient in UIHandlersCopy)
            {
                await NotifyUI(uiClient, changedElement);
            }
        }

        private T? CastTo<T>(dynamic objectToConvert)
        {
            try
            {
                T element = (T)objectToConvert;
                return element;
            }
            catch
            {
                return default(T);
            }
        }
    }
}
