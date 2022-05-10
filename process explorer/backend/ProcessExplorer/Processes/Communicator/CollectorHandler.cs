/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector;
using ProcessExplorer.LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.Processes.Communicator;

public class CollectorHandler : ICommunicator
{
    private IProcessInfoAggregator aggregator;

    public CollectorHandler(IProcessInfoAggregator processAggregator)
    {
        this.aggregator = processAggregator;
    }

    #region Setters
    public void SetProcessInfoAggregator(IProcessInfoAggregator aggregator)
        => this.aggregator = aggregator;
    #endregion

    public async Task AddRuntimeInfo(AssemblyInformation assemblyId, ProcessInfoCollectorData dataObject)
    {
        await aggregator.AddInformation(assemblyId.Name, dataObject);
    }

    public async Task AddConnectionCollection(AssemblyInformation assemblyId, IEnumerable<ConnectionInfo> connections)
    {
        await aggregator.AddConnectionCollection(assemblyId.Name, connections);
    }

    public async Task UpdateConnectionInformation(AssemblyInformation assemblyId, ConnectionInfo connection)
    {
        await aggregator.UpdateConnectionInfo(assemblyId.Name, connection);
    }

    public async Task UpdateEnvironmentVariableInformation(AssemblyInformation assemblyId, IEnumerable<KeyValuePair<string,string>> environmentVariables)
    {
        await aggregator.UpdateEnvironmentVariablesInfo(assemblyId.Name, environmentVariables);
    }

    public async Task UpdateRegistrationInformation(AssemblyInformation assemblyId, IEnumerable<RegistrationInfo> registrations)
    {
        await aggregator.UpdateRegistrationInfo(assemblyId.Name, registrations);
    }

    public async Task UpdateModuleInformation(AssemblyInformation assemblyId, IEnumerable<ModuleInfo> modules)
    {
        await aggregator.UpdateModuleInfo(assemblyId.Name, modules);
    }

}
