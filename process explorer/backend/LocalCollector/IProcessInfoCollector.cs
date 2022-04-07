/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.LocalCollector
{
    public interface IProcessInfoCollector
    {
        ProcessInfoCollectorData Data { get; }
        Task AddConnectionMonitor(ConnectionMonitorInfo connections);
        Task AddConnectionMonitor(ConnectionMonitor connections);
        Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables);
        Task AddRegistrations(RegistrationMonitorInfo registrations);
        Task AddModules(ModuleMonitorInfo modules);
        Task AddRuntimeInformation(ConnectionMonitor connections, EnvironmentMonitorInfo environmentVariables,
            RegistrationMonitorInfo registrations, ModuleMonitorInfo modules);
        void SetCommunicator(ICommunicator communicator);
        Task SendRuntimeInfo();
        void SetAssemblyID(string assemblyID);
        void SetClientPID(int clientPID);
    }
}
