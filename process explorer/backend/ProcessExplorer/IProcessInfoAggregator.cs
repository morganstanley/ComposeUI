/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Processes;
using System.Collections.Concurrent;
using ProcessExplorer.LocalCollector;
using ProcessExplorer.Processes.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Registrations;
using ProcessExplorer.LocalCollector.Modules;

namespace ProcessExplorer
{
    public interface IProcessInfoAggregator
    {
        #region Properties
        /// <summary>
        /// Contains information.
        /// (connection/registrations/modules/environment variables)
        /// </summary>
        ConcurrentDictionary<string, ProcessInfoCollectorData>? Information { get; }

        /// <summary>
        /// Contains and collects the information about the related processes to the Compose.
        /// </summary>
        IProcessMonitor? ProcessMonitor { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a runtime information to the collection.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="processInfo"></param>
        Task AddInformation(string assemblyId, ProcessInfoCollectorData processInfo);

        /// <summary>
        /// Removes a module information from the collection.
        /// </summary>
        /// <param name="assembly"></param>
        void RemoveAInfoAggregatorInformation(string assembly);

        /// <summary>
        /// Sets Compose PID.
        /// </summary>
        /// <param name="pid"></param>
        void SetComposePID(int pid);

        /// <summary>
        /// Sets the delay time for keeping a process after it was terminated.(s)
        /// Default: 1 minute.
        /// </summary>
        /// <param name="delay"></param>
        void SetDeadProcessRemovalDelay(int delay);

        /// <summary>
        /// Reinitialized the list containing the current, relevant processes
        /// </summary>
        /// <returns>A collection</returns>
        SynchronizedCollection<ProcessInfoData>? RefreshProcessList();

        /// <summary>
        /// Returns the list containing the processes.
        /// </summary>
        /// <returns></returns>
        SynchronizedCollection<ProcessInfoData>? GetProcesses();

        /// <summary>
        /// Initializes the Process Monitor and fills the list -containing the related processes.
        /// </summary>
        void InitProcessExplorer();

        /// <summary>
        /// Initializes the process creator/modifier/terminator actions.
        /// </summary>
        void SetWatcher();

        /// <summary>
        /// Adds a UIClient to the collection. Keeps track of the UIClients.
        /// </summary>
        /// <param name="UIHandler"></param>
        void AddUIConnection(IUIHandler UIHandler);

        /// <summary>
        /// Adds or updates the connections in the collection.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="connections"></param>
        Task AddConnectionCollection(string assemblyId, SynchronizedCollection<ConnectionInfo> connections);

        /// <summary>
        /// Updates a connection.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="connectionInfo"></param>
        Task UpdateConnectionInfo(string assemblyId, ConnectionInfo connectionInfo);

        /// <summary>
        /// Updates the environment variables.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="environmentVariables"></param>
        Task UpdateEnvironmentVariablesInfo(string assemblyId, EnvironmentMonitorInfo environmentVariables);

        /// <summary>
        /// Updates the registrations in the collection.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="registrations"></param>
        Task UpdateRegistrationInfo(string assemblyId, RegistrationMonitorInfo registrations);

        /// <summary>
        /// Updates the modules in the collection.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="modules"></param>
        Task UpdateModuleInfo(string assemblyId, ModuleMonitorInfo modules);
        #endregion
    }
}
