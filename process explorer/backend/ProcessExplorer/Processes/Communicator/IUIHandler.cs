/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.Processes.Communicator;

public interface IUIHandler
{
    Task AddProcesses(IEnumerable<ProcessInfoData>? processes);
    Task AddProcess(ProcessInfoData process);
    Task UpdateProcess(ProcessInfoData process);
    Task RemoveProcess(int pid);

    Task AddRuntimeInfo(ProcessInfoCollectorData dataObject);
    Task AddConnections(IEnumerable<ConnectionInfo> connections);
    Task UpdateConnection(ConnectionInfo connection);
    Task UpdateEnvironmentVariables(IEnumerable<KeyValuePair<string,string>> environmentVariables);
    Task UpdateRegistrations(IEnumerable<RegistrationInfo> registrations);
    Task UpdateModules(IEnumerable<ModuleInfo> modules);
}
