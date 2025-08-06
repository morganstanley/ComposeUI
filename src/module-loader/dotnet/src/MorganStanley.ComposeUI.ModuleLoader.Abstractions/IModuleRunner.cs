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
/// Defines the contract for running modules, including starting and stopping module instances of a specific type.
/// </summary>
public interface IModuleRunner
{
    /// <summary>
    /// Gets the type of the module that this runner supports (e.g., "Web", "Native").
    /// </summary>
    public string ModuleType { get; }

    /// <summary>
    /// Starts a module instance using the provided <see cref="StartupContext"/> and executes the specified startup pipeline.
    /// </summary>
    /// <param name="startupContext">The context containing information and properties for module startup.</param>
    /// <param name="pipeline">A delegate representing the next step in the startup pipeline.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public Task Start(StartupContext startupContext, Func<Task> pipeline);

    /// <summary>
    /// Stops the specified module instance.
    /// </summary>
    /// <param name="moduleInstance">The module instance to stop.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public Task Stop(IModuleInstance moduleInstance);
}
