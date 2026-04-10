/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client;

/// <summary>
/// Shell specific factory which will be used by the Fdc3DesktopAgent to retrieve the IDesktopAgent implementation that will be used for Fdc3 integration. This is needed to abstract away how the DesktopAgentClient can be resolved.
/// </summary>
public interface IDesktopAgentClientFactory
{
    /// <summary>
    /// Returns the IDesktopAgent implementation that will be used by the Fdc3DesktopAgent for Fdc3 integration. The implementation of this factory should handle the resolution of the IDesktopAgent, whether it's through a service locator, dependency injection, or any other method. This allows the DesktopAgentClient to remain decoupled from the specifics of how the IDesktopAgent is provided, enabling greater flexibility and testability.
    /// </summary>
    /// <param name="identifier">Unique identifier of the module</param>
    /// <param name="onReady">Callback which signals that the desktop agent client is ready for the module.</param>
    /// <returns></returns>
    public Task GetDesktopAgentAsync(string identifier, Action<IDesktopAgent> onReady);

    /// <summary>
    /// Registers the Fdc3StartupProperties for in-process apps, which can be used to provide necessary information about the app to the DesktopAgentClient for proper Fdc3 integration. 
    /// This helps to identify the different native modules and provides ability to create the DesktopAgent client which can be used for in process apps to collaborate in the FDC3 ecosystem. 
    /// The implementation of this method should handle the storage and management of the Fdc3StartupProperties for in-process apps, ensuring that the DesktopAgentClient can access this information when needed for Fdc3 operations. 
    /// This is particularly important for scenarios where multiple in-process apps are running and need to be distinguished from each other for proper Fdc3 functionality.
    /// </summary>
    /// <param name="fdc3Properties"></param>
    public Task RegisterInProcessAppPropertiesAsync(Fdc3StartupProperties fdc3Properties);
}
