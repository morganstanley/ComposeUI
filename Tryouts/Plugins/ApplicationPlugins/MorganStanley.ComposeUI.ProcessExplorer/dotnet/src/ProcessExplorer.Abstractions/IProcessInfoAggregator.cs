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

using ProcessExplorer.Abstractions.Entities;
using ProcessExplorer.Abstractions.Entities.Connections;
using ProcessExplorer.Abstractions.Entities.Modules;
using ProcessExplorer.Abstractions.Entities.Registrations;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;

namespace ProcessExplorer.Abstractions;

public interface IProcessInfoAggregator : IDisposable
{
    /// <summary>
    /// Main process id to watch.
    /// </summary>
    public int MainProcessId { get; }

    /// <summary>
    /// Delay for actually sending terminate request for the UI.
    /// </summary>
    public int TerminatingProcessDelay { get; }

    /// <summary>
    /// Controls the initialized subsystems.
    /// </summary>
    public ISubsystemController? SubsystemController { get; }

    /// <summary>
    /// Handles the communication between the server and clients.
    /// </summary>
    public IUiHandler UiHandler { get; }

    /// <summary>
    /// Removes a module information from the collection.
    /// </summary>
    /// <param name="assembly"></param>
    void RemoveRuntimeInformation(string assembly);

    /// <summary>
    /// Sets Compose PID.
    /// </summary>
    /// <param name="processId"></param>
    void SetMainProcessId(int processId);

    /// <summary>
    /// Sets the SubsystemController.
    /// </summary>
    /// <param name="subsystemController"></param>
    void SetSubsystemController(ISubsystemController subsystemController);

    /// <summary>
    /// Sets the delay time for keeping a process after it was terminated.(s)
    /// Default: 1 minute.
    /// </summary>
    /// <param name="delay"></param>
    void SetDeadProcessRemovalDelay(int delay);
    
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
    /// <param name="processIds"></param>
    void InitProcesses(ReadOnlySpan<int> processIds);

    /// <summary>
    /// Puts the given subsystem into the queue to send subsystem state changed information to the UI's.
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="state"></param>
    void ScheduleSubsystemStateChanged(Guid instanceId, string state);

    /// <summary>
    /// Returns the initialized runtime information.
    /// </summary>
    /// <returns></returns>
    IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> GetRuntimeInformation();
    
    /// <summary>
    /// Adds a runtime information to the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="processInfo"></param>
    Task AddRuntimeInformation(string assemblyId, ProcessInfoCollectorData processInfo);

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
    Task UpdateOrAddConnectionInfo(string assemblyId, ConnectionInfo connectionInfo);

    /// <summary>
    /// Updates the environment variables.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="environmentVariables"></param>
    Task UpdateOrAddEnvironmentVariablesInfo(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables);

    /// <summary>
    /// Adds the registrations to the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="registrations"></param>
    Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations);

    /// <summary>
    /// Updates the modules to the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="modules"></param>
    Task UpdateOrAddModuleInfo(string assemblyId, IEnumerable<ModuleInfo> modules);

    /// <summary>
    /// Adds processes to watch to the existing watchable process ids list.
    /// </summary>
    /// <param name="processIds"></param>
    /// <returns></returns>
    Task AddProcesses(ReadOnlySpan<int> processIds);

    /// <summary>
    /// Asynchronously dequeue the changes of the registered subsystems, and send to the initialized UI's.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RunSubsystemStateQueue(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the initialized processes.
    /// </summary>
    /// <returns></returns>
    IEnumerable<ProcessInfoData> GetProcesses();
}
