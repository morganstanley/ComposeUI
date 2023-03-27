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
using LocalCollector.Modules;
using LocalCollector.Registrations;
using ProcessExplorer.Abstractions.Processes;
using ConnectionInfo = LocalCollector.Connections.ConnectionInfo;

namespace SuperRPC_POC.Protocol.ProxyObjects;

public interface IProcessAggregatorUIServiceObject
{
    Task AddRuntimeInfo(string assemblyId, ProcessInfoCollectorData dataObject);
    Task AddRuntimeInfos(IEnumerable<KeyValuePair<string, ProcessInfoCollectorData>> runtimeInfos);
    Task AddConnections(string assemblyId, IEnumerable<ConnectionInfo> connections);
    Task AddConnection(string assemblyId, ConnectionInfo connection);
    Task UpdateConnection(string assemblyId, ConnectionInfo connection);
    Task UpdateEnvironmentVariables(string assemblyId, IEnumerable<KeyValuePair<string,string>> environmentVariables);
    Task UpdateRegistrations(string assemblyId, IEnumerable<RegistrationInfo> registrations);
    Task UpdateModules(string assemblyId, IEnumerable<ModuleInfo> modules);
    Task AddProcesses(IEnumerable<ProcessInfoData>? processes);
    Task AddProcess(ProcessInfoData process);
    Task UpdateProcess(ProcessInfoData process);
    Task TerminateProcess(int pid);
}
