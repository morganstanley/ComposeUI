/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Communicator;
using ProcessExplorer.LocalCollector;
using ProcessExplorer.LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
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
        aggregator.AddInformation(assemblyId.Name, dataObject);
    }

    public async Task AddConnectionCollection(AssemblyInformation assemblyId, SynchronizedCollection<ConnectionInfo> connections)
    {
        aggregator.AddConnectionCollection(assemblyId.Name, connections);
    }

    public async Task UpdateConnectionInformation(AssemblyInformation assemblyId, ConnectionInfo connection)
    {
        aggregator.UpdateConnectionInfo(assemblyId.Name, connection);
    }

    public async Task UpdateEnvironmentVariableInformation(AssemblyInformation assemblyId, EnvironmentMonitorInfo environmentVariables)
    {
        aggregator.UpdateEnvironmentVariablesInfo(assemblyId.Name, environmentVariables);
    }

    public async Task UpdateRegistrationInformation(AssemblyInformation assemblyId, RegistrationMonitorInfo registrations)
    {
        aggregator.UpdateRegistrationInfo(assemblyId.Name, registrations);
    }

    public async Task UpdateModuleInformation(AssemblyInformation assemblyId, ModuleMonitorInfo modules)
    {
        aggregator.UpdateModuleInfo(assemblyId.Name, modules);
    }

}
