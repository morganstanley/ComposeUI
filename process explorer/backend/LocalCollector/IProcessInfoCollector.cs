/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.Connections.Interfaces;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.LocalCollector
{
    public interface IProcessInfoCollector
    {
        /// <summary>
        /// Contains information of the environment variables, conenctions, registrations, modules
        /// </summary>
        ProcessInfoCollectorData Data { get; }

        /// <summary>
        /// Adds a list of connections to the existing one.
        /// </summary>
        /// <param name="connections"></param>
        /// <returns></returns>
        Task AddConnectionMonitor(ConnectionMonitorInfo connections);

        /// <summary>
        /// Adds a connection monitor to watch connections.
        /// </summary>
        /// <param name="connections"></param>
        /// <returns></returns>
        Task AddConnectionMonitor(IConnectionMonitor connections);

        /// <summary>
        /// Adds a list of environment variables.
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <returns></returns>
        Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables);

        /// <summary>
        /// Adds a list of registrations.
        /// </summary>
        /// <param name="registrations"></param>
        /// <returns></returns>
        Task AddRegistrations(RegistrationMonitorInfo registrations);

        /// <summary>
        /// Adds a list of modules.
        /// </summary>
        /// <param name="modules"></param>
        /// <returns></returns>
        Task AddModules(ModuleMonitorInfo modules);

        /// <summary>
        /// Adds information of conenctions/environment variables/registrations/modules to the colelction.
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="environmentVariables"></param>
        /// <param name="registrations"></param>
        /// <param name="modules"></param>
        /// <returns></returns>
        Task AddRuntimeInformation(IConnectionMonitor connections, EnvironmentMonitorInfo environmentVariables,
            RegistrationMonitorInfo registrations, ModuleMonitorInfo modules);

        /// <summary>
        /// Sets communicator, which talks with the Process Explorer backend. Also after the conenction initialized it will send the data of the existing collections.
        /// </summary>
        /// <param name="communicator"></param>
        void SetCommunicator(ICommunicator communicator);

        /// <summary>
        /// Sends the runtime information of the current process.
        /// </summary>
        /// <returns></returns>
        Task SendRuntimeInfo();

        /// <summary>
        /// Sets the name of the assembly, potentially it is the assembly name of the current running process, what we want to send to the backend.
        /// </summary>
        /// <param name="assemblyID"></param>
        void SetAssemblyID(string assemblyID);

        /// <summary>
        /// Sets the PID of the current running process, what we want to send to the Process Explorer backend.
        /// </summary>
        /// <param name="clientPID"></param>
        void SetClientPID(int clientPID);
    }
}
