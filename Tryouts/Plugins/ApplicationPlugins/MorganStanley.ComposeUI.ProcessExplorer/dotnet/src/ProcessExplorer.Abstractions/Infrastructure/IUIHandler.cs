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
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;

namespace ProcessExplorer.Abstractions.Infrastructure;

public interface IUIHandler
{
    /// <summary>
    /// Adds a list of processes to the collection.
    /// </summary>
    /// <param name="processes"></param>
    /// <returns></returns>
    Task AddProcesses(IEnumerable<ProcessInfoData> processes);

    /// <summary>
    /// Adds a process to the collection.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Task AddProcess(ProcessInfoData process);

    /// <summary>
    /// Modifies the information of the given process in the collection.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Task UpdateProcess(ProcessInfoData process);

    /// <summary>
    /// Removes process from the collection.
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    Task TerminateProcess(int pid);

    /// <summary>
    /// Collects runtime information to the collection. (Environment variables/modules/connections/registrations)
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="dataObject"></param>
    /// <returns></returns>
    Task AddRuntimeInfo(string assemblyId, ProcessInfoCollectorData dataObject);

    /// <summary>
    /// Adds a collection of runtime information to the collection. (List of environment variables/modules/connections/registrations)
    /// </summary>
    /// <param name="runtimeInfo"></param>
    /// <returns></returns>
    Task AddRuntimeInfo(IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> runtimeInfo);

    /// <summary>
    /// Adds a collection of connections to the main collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="connections"></param>
    /// <returns></returns>
    Task AddConnections(string assemblyId, IEnumerable<ConnectionInfo> connections);

    /// <summary>
    /// Updates an information of connection in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    Task UpdateConnection(string assemblyId, ConnectionInfo connection);

    /// <summary>
    /// Updates information of environment variables in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="environmentVariables"></param>
    /// <returns></returns>
    Task UpdateEnvironmentVariables(string assemblyId, IEnumerable<KeyValuePair<string, string>> environmentVariables);

    /// <summary>
    /// Updates information of registrations in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="registrations"></param>
    /// <returns></returns>
    Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations);

    /// <summary>
    /// Updates information of modules in the collection.
    /// </summary>
    /// <param name="assemblyId"></param>
    /// <param name="modules"></param>
    /// <returns></returns>
    Task UpdateModules(string assemblyId, IEnumerable<ModuleInfo> modules);

    //SUBSYSTEM PART
    /// <summary>
    /// Adds a collection of subsystems.
    /// </summary>
    /// <param name="subsystems"></param>
    /// <returns></returns>
    Task AddSubsystems(IEnumerable<KeyValuePair<Guid, SubsystemInfo>> subsystems);

    /// <summary>
    /// Adds a subsystem to the collection on the UI.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystem"></param>
    /// <returns></returns>
    Task AddSubsystem(Guid subsystemId, SubsystemInfo subsystem);

    /// <summary>
    /// Update an information of subsystem.
    /// Sends a message to the UI - probably that the state of the Subsystem is changed.
    /// </summary>
    /// <param name="subsystemId"></param>
    /// <param name="subsystem"></param>
    /// <returns></returns>
    Task UpdateSubsystemInfo(Guid subsystemId, SubsystemInfo subsystem);

    /// <summary>
    /// Updates the status of the process. Mostly used for sending update terminated process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    Task UpdateProcessStatus(KeyValuePair<int, Status> process);
}
