/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using ProcessExplorer.Entities.Connections;
using ProcessExplorer.Entities.EnvironmentVariables;
using ProcessExplorer.Entities.Registrations;

namespace ProcessExplorer
{
    public interface IInfoAggregator
    {
        InfoAggregatorDto Data { get; set; }
        void AddConnectionMonitor(ConnectionMonitorDto connections);
        void AddConnectionMonitor(ConnectionMonitor connections);
        void AddEnvironmentVariables(EnvironmentMonitorDto envrionmentVariables);
        void AddRegistrations(RegistrationMonitorDto registrations);
        void AddModules(ModuleMonitorDto modules);
        void AddInformation(ConnectionMonitor connections, EnvironmentMonitorDto envrionmentVariables,
            RegistrationMonitorDto registrations, ModuleMonitorDto modules);
    }
}