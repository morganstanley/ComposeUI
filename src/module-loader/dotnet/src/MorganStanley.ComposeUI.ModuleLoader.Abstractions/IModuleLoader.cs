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

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Defines the contract for loading, starting, stopping, and monitoring modules within the application.
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// Starts a module using the specified <see cref="StartRequest"/>.
    /// </summary>
    /// <param name="startRequest">The request containing the module identifier and configuration parameters for starting the module.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="IModuleInstance"/> representing the started module.
    /// </returns>
    public Task<IModuleInstance> StartModule(StartRequest startRequest);

    /// <summary>
    /// Stops a running module instance using the specified <see cref="StopRequest"/>.
    /// </summary>
    /// <param name="stopRequest">The request containing the instance identifier and optional properties for stopping the module.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public Task StopModule(StopRequest stopRequest);

    /// <summary>
    /// Gets an observable sequence of lifetime events for all managed modules.
    /// Subscribers can monitor module start, stop, and other lifecycle events.
    /// </summary>
    public IObservable<LifetimeEvent> LifetimeEvents { get; }
}
