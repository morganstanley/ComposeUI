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
using LocalCollector.Communicator;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using ProcessExplorer.Abstractions;

namespace ProcessExplorer.Core.Infrastructure;

//TODO(Lilla): refactor so it is longer using proxy based callings
internal class Communicator : ICommunicator
{
    private IProcessInfoAggregator _aggregator;

    public Communicator(IProcessInfoAggregator processAggregator)
    {
        _aggregator = processAggregator;
    }

    public async ValueTask AddRuntimeInfo(IEnumerable<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>> listOfRuntimeInfos)
    {
        if (listOfRuntimeInfos == null) return;

        foreach (var runtimeInfo in listOfRuntimeInfos)
        {
            if (runtimeInfo.Value != null && runtimeInfo.Key.Name != string.Empty)
            {
                await _aggregator.AddRuntimeInformation(runtimeInfo.Key.Name, runtimeInfo.Value);
            }
        }
    }

    public async ValueTask AddConnectionCollection(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ConnectionInfo>>> connections)
    {
        if (connections == null) return;

        foreach (var connection in connections)
        {
            if (connection.Value == null || connection.Key.Name == string.Empty) continue;
            await _aggregator.AddConnectionCollection(connection.Key.Name, connection.Value);
        }
    }

    public async ValueTask UpdateConnectionInformation(IEnumerable<KeyValuePair<AssemblyInformation, ConnectionInfo>> connections)
    {
        if (connections == null) return;

        foreach (var connection in connections)
        {
            if (connection.Value == null || connection.Key.Name == string.Empty) continue;
            await _aggregator.UpdateOrAddConnectionInfo(connection.Key.Name, connection.Value);
        }
    }

    public async ValueTask UpdateEnvironmentVariableInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<KeyValuePair<string, string>>>> environmentVariables)
    {
        if (environmentVariables == null) return;

        foreach (var env in environmentVariables)
        {
            if (env.Value == null || env.Key.Name == string.Empty) continue;
            await _aggregator.UpdateOrAddEnvironmentVariablesInfo(env.Key.Name, env.Value);
        }
    }

    public async ValueTask UpdateRegistrationInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<RegistrationInfo>>> registrations)
    {
        if (registrations == null) return;

        foreach (var registration in registrations)
        {
            if (registration.Value == null || registration.Key.Name == string.Empty) continue;
            await _aggregator.UpdateRegistrations(registration.Key.Name, registration.Value);
        }
    }

    public async ValueTask UpdateModuleInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ModuleInfo>>> modules)
    {
        if (modules == null) return;

        foreach (var module in modules)
        {
            if (module.Value == null || module.Key.Name == string.Empty) continue;
            await _aggregator.UpdateOrAddModuleInfo(module.Key.Name, module.Value);
        }
    }
}
