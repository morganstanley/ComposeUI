/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.LocalCollector.Communicator
{
    public interface ICommunicator
    {
        /// <summary>
        /// Adds the collected runtime information to the Process Explorer backend.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="dataObject"></param>
        /// <returns></returns>
        Task AddRuntimeInfo(AssemblyInformation assemblyId, ProcessInfoCollectorData dataObject);

        /// <summary>
        /// Sends a message to the UI, if a new list of connections has been added.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        Task AddConnectionCollection(AssemblyInformation assemblyId, SynchronizedCollection<ConnectionInfo> connections);

        /// <summary>
        /// Sends a message to the UI, if a connection has been updated.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        Task UpdateConnectionInformation(AssemblyInformation assemblyId, ConnectionInfo connection);

        /// <summary>
        /// Sends a message to the UI, if the environment variables of the collector has been updated.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        Task UpdateEnvironmentVariableInformation(AssemblyInformation assemblyId, EnvironmentMonitorInfo environmentVariables);

        /// <summary>
        /// Sends a message to the UI, if the registrations of the collector has been updated.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="registrations"></param>
        /// <returns></returns>
        Task UpdateRegistrationInformation(AssemblyInformation assemblyId, RegistrationMonitorInfo registrations);

        /// <summary>
        /// Sends a message to the UI, if the modules of the collector has been updated.
        /// </summary>
        /// <param name="assemblyId"></param>
        /// <param name="modules"></param>
        /// <returns></returns>
        Task UpdateModuleInformation(AssemblyInformation assemblyId, ModuleMonitorInfo modules);
    }
}
