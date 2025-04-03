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

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

/// <summary>
/// Defines the interface for managing keyboard shortcuts.
/// </summary>
public interface IShortcut
{
    /// <summary>
    /// Registers a keyboard shortcut for the specified application.
    /// </summary>
    /// <param name="appId">The ID of the application.</param>
    /// <param name="shortcut">The keyboard shortcut to register.</param>
    /// <param name="callback">The callback to invoke when the shortcut is triggered.</param>
    void Register(string appId, string shortcut, object callback);

    /// <summary>
    /// Checks if a keyboard shortcut is registered for the specified application.
    /// </summary>
    /// <param name="appId">The ID of the application.</param>
    /// <param name="shortcut">The keyboard shortcut to check.</param>
    void IsRegistered(string appId, string shortcut);

    /// <summary>
    /// Unregisters a keyboard shortcut for the specified application.
    /// </summary>
    /// <param name="appId">The ID of the application.</param>
    /// <param name="shortcut">The keyboard shortcut to unregister.</param>
    void Unregister(string appId, string shortcut);

    /// <summary>
    /// Unregisters all keyboard shortcuts for the specified application.
    /// </summary>
    /// <param name="appId">The ID of the application.</param>
    void UnregisterAll(string appId);
}