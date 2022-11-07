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
using ModuleProcessMonitor.Processes;
using ProcessExplorer.Infrastructure;

namespace ProcessExplorer;

public interface IProcessInfoAggregator
{
    /// <summary>
    /// Contains information.
    /// (connection/registrations/modules/environment variables)
    /// </summary>
    ConcurrentDictionary<string, ProcessInfoCollectorData>? ProcessInformation { get; }

    /// <summary>
    /// Adds a compose process to keep track on it.
    /// </summary>
    /// <param name="processInfo"></param>
    void AddProcessInfo(ProcessInfoData processInfo);

    /// <summary>
    /// Contains and collects the information about the related processes in the Compose.
    /// </summary>
    IProcessMonitor? ProcessMonitor { get; }

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
    void SetComposePid(int pid);

    /// <summary>
    /// Sets ProcessMonitor instance.
    /// </summary>
    /// <param name="processMonitor"></param>
    Task SetProcessMonitor(IProcessMonitor processMonitor);

    /// <summary>
    /// Sets the delay time for keeping a process after it was terminated.(s)
    /// Default: 1 minute.
    /// </summary>
    /// <param name="delay"></param>
    void SetDeadProcessRemovalDelay(int delay);

    /// <summary>
    /// Returns the list containing the processes.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ProcessInfoData>? GetProcesses();

    /// <summary>
    /// Adds a UIClient to the collection. Keeps track of the UIClients.
    /// </summary>
    /// <param name="uiHandler"></param>
    void AddUiConnection(IUIHandler uiHandler);

    /// <summary>
    /// Removes  a UIClient from the collection.
    /// </summary>
    /// <param name="uiHandler"></param>
    void RemoveUiConnection(IUIHandler uiHandler);

    /// <summary>
    /// Adds or updates the connections in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="connections"></param>
    Task AddConnectionCollection(string assemblyId, IEnumerable<ConnectionInfo> connections);

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
    Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string,string>> environmentVariables);

    /// <summary>
    /// Updates the registrations in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="registrations"></param>
    Task UpdateRegistrationInfo(string assemblyId, IEnumerable<RegistrationInfo> registrations);

    /// <summary>
    /// Updates the modules in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="modules"></param>
    Task UpdateModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules);

    /// <summary>
    /// Terminates a process by ID.
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    Task RemoveProcessById(int pid);

    /// <summary>
    /// Enables to watch processes through ProcessMonitor.
    /// Only available for Windows OS.
    /// </summary>
    void EnableWatchingSavedProcesses();

    /// <summary>
    /// Disables to watch processes.
    /// </summary>
    void DisableWatchingProcesses();

    /// <summary>
    /// Sets the processes, which is gotten from the ModuleLoader.
    /// </summary>
    /// <param name="processInfo"></param>
    void InitProcesses(IEnumerable<ProcessInfoData> processInfo);
}
