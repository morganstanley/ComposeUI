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

using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;

namespace MorganStanley.ComposeUI.ProcessExplorer.Client;

public interface IProcessInfoHandler
{
    /// <summary>
    /// Adds a list of connections to the existing one.
    /// </summary>
    /// <param name="connections"></param>
    /// <returns></returns>
    ValueTask AddConnections(IEnumerable<IConnectionInfo> connections);

    /// <summary>
    /// Adds a list of environment variables.
    /// </summary>
    /// <param name="environmentVariables"></param>
    /// <returns></returns>
    ValueTask AddEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> environmentVariables);

    /// <summary>
    /// Adds a list of registrations.
    /// </summary>
    /// <param name="registrations"></param>
    /// <returns></returns>
    ValueTask AddRegistrations(IEnumerable<RegistrationInfo> registrations);

    /// <summary>
    /// Adds a list of modules.
    /// </summary>
    /// <param name="modules"></param>
    /// <returns></returns>
    ValueTask AddModules(IEnumerable<ModuleInfo> modules);

    /// <summary>
    /// Adds information of connections/environment variables/registrations/modules to the colelction.
    /// </summary>
    /// <param name="connections"></param>
    /// <param name="environmentVariables"></param>
    /// <param name="registrations"></param>
    /// <param name="modules"></param>
    /// <returns></returns>
    ValueTask AddRuntimeInformation(
        IEnumerable<IConnectionInfo> connections,
        IEnumerable<KeyValuePair<string, string>> environmentVariables,
        IEnumerable<RegistrationInfo> registrations,
        IEnumerable<ModuleInfo> modules);

    /// <summary>
    /// Sends the runtime information of the current process.
    /// </summary>
    /// <returns></returns>
    ValueTask SendRuntimeInfo();

    /// <summary>
    /// Sets the name of the assembly, potentially it is the assembly name of the current running process, what we want to send to the backend.
    /// </summary>
    /// <param name="assemblyId"></param>
    void SetAssemblyId(string assemblyId);

    /// <summary>
    /// Sets the PID of the current running process, what we want to send to the Process Explorer backend.
    /// </summary>
    /// <param name="clientPid"></param>
    void SetClientPid(int clientPid);

    /// <summary>
    /// Returns the ProcessInfoColelctorData object.
    /// </summary>
    /// <returns></returns>
    ProcessInfoCollectorData GetProcessInfoCollectorData();
}
