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

using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using ProcessExplorer.Abstraction.Infrastructure;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Abstraction.Subsystems;

namespace ProcessExplorer.Abstraction;

public interface IProcessInfoAggregator : IDisposable
{
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
    void RemoveRuntimeInformation(string assembly);

    /// <summary>
    /// Sets Compose PID.
    /// </summary>
    /// <param name="pid"></param>
    void SetComposePid(int pid);

    /// <summary>
    /// Sets the SubsystemController.
    /// </summary>
    /// <param name="subsystemController"></param>
    void SetSubsystemController(ISubsystemController subsystemController);
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
    Task UpdateEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables);

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
    void InitProcesses(ReadOnlySpan<int> pids);

    /// <summary>
    /// Initializes the subsystems taken from the user defined manifest.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task InitializeSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems);

    /// <summary>
    /// Terminates the given subsystems through the user defined ISubsystemLauncher.
    /// </summary>
    /// <param name="subsystemIds"></param>
    /// <returns></returns>
    Task ShutdownSubsystems(IEnumerable<string> subsystemIds);

    /// <summary>
    /// Restarts the given subsystems through the user defined ISubsystemLauncher.
    /// </summary>
    /// <param name="subsystemIds"></param>
    /// <returns></returns>
    Task RestartSubsystems(IEnumerable<string> subsystemIds);

    /// <summary>
    /// Launch the given subsystems through the user defined ISubsystemLauncher.
    /// </summary>
    /// <param name="subsystemIds"></param>
    /// <returns></returns>
    Task LaunchSubsystems(IEnumerable<string> subsystemIds);

    /// <summary>
    /// Launch the given subsystem through the user defined ISubsystemLauncher and <paramref name="periodOfTime"/>.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="periodOfTime"></param>
    /// <returns></returns>
    Task LaunchSubsystemWithDelay(Guid id, int periodOfTime);

    /// <summary>
    /// Modifies a state of a subsystem with the given data. Send update to the registered UIs.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    Task ModifySubsystemState(Guid subsystemId, string state);
    void ScheduleSubsystemStateChanged(Guid instanceId, string state);
}
