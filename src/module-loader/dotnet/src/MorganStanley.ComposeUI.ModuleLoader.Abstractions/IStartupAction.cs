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
/// Defines a startup action that can be executed as part of a module's initialization pipeline.
/// Implementations can perform custom logic before or after the module is started.
/// </summary>
public interface IStartupAction
{
    /// <summary>
    /// Invokes the startup action using the provided <see cref="StartupContext"/>.
    /// Implementations should call <paramref name="next"/> to continue the startup pipeline.
    /// </summary>
    /// <param name="startupContext">The context containing information about the module startup.</param>
    /// <param name="next">A delegate to invoke the next action in the startup pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InvokeAsync(StartupContext startupContext, Func<Task> next);
}