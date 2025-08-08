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


namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Provides a callback mechanism to handle unexpected stops of a module instance.
/// </summary>
public class UnexpectedStopCallback
{
    private IModuleInstance _instance;
    private Action<IModuleInstance> _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedStopCallback"/> class.
    /// </summary>
    /// <param name="instance">The module instance to monitor for unexpected stops.</param>
    /// <param name="callback">The callback action to invoke when the module instance stops unexpectedly.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="instance"/> or <paramref name="callback"/> is <c>null</c>.
    /// </exception>
    public UnexpectedStopCallback(IModuleInstance instance, Action<IModuleInstance> callback)
    {
        _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Invokes the callback to handle the unexpected stop event for the associated module instance.
    /// </summary>
    /// <param name="sender">The source of the event (not used).</param>
    /// <param name="e">The event data (not used).</param>
    public void ProcessStoppedUnexpectedly(object? sender, EventArgs? e)
    {
        _callback?.Invoke(_instance);
    }
}
