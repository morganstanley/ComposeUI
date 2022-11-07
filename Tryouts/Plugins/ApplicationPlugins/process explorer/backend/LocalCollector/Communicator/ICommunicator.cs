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

using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;

namespace LocalCollector.Communicator;

public interface ICommunicator
{
    /// <summary>
    /// Adds the collected runtime information to the Process Explorer backend.
    /// </summary>
    /// <param name="listOfRuntimeInfos"></param>
    /// <returns></returns>
    ValueTask AddRuntimeInfo(IEnumerable<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>> listOfRuntimeInfos);

    /// <summary>
    /// Sends a message to the UI, if a new list of connections has been added.
    /// </summary>
    /// <param name="connections"></param>
    /// <returns></returns>
    ValueTask AddConnectionCollection(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ConnectionInfo>>> connections);

    /// <summary>
    /// Sends a message to the UI, if a connection has been updated.
    /// </summary>
    /// <param name="connections"></param>
    /// <returns></returns>
    ValueTask UpdateConnectionInformation(IEnumerable<KeyValuePair<AssemblyInformation, ConnectionInfo>> connections);

    /// <summary>
    /// Sends a message to the UI, if the environment variables of the collector has been updated.
    /// </summary>
    /// <param name="environmentVariables"></param>
    /// <returns></returns>
    ValueTask UpdateEnvironmentVariableInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<KeyValuePair<string, string>>>> environmentVariables);

    /// <summary>
    /// Sends a message to the UI, if the registrations of the collector has been updated.
    /// </summary>
    /// <param name="registrations"></param>
    /// <returns></returns>
    ValueTask UpdateRegistrationInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<RegistrationInfo>>> registrations);

    /// <summary>
    /// Sends a message to the UI, if the modules of the collector has been updated.
    /// </summary>
    /// <param name="modules"></param>
    /// <returns></returns>
    ValueTask UpdateModuleInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ModuleInfo>>> modules);
}
