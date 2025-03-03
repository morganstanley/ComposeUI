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
/// Represents a shutdown action which should be called when a module instance closes.
/// </summary>
public interface IShutdownAction
{
    /// <summary>
    /// Calls the shutdown action.
    /// </summary>
    /// <param name="shutDownContext">Stores the module instance and parameters for the shutdown action.</param>
    /// <param name="next">Next action.</param>
    /// <returns></returns>
    Task InvokeAsync(ShutdownContext shutDownContext, Func<Task> next);
}
