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

public interface IModuleLoader
{
    /// <summary>
    /// Starts the module.
    /// </summary>
    /// <param name="startRequest">Provides possibility to pass different configuration for starting the module.</param>
    /// <returns></returns>
    public Task<IModuleInstance> StartModule(StartRequest startRequest);


    /// <summary>
    /// Stops the module by instance id.
    /// </summary>
    /// <param name="stopRequest"></param>
    /// <returns></returns>
    public Task StopModule(StopRequest stopRequest);


    /// <summary>
    /// The module's lifetime events through its lifetime.
    /// </summary>
    public IObservable<LifetimeEvent> LifetimeEvents { get; }
}
