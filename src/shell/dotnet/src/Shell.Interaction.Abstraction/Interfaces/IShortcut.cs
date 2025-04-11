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

using MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

/// <summary>
/// Defines the interface for managing keyboard shortcuts.
/// </summary>
public interface IShortcut
{
    /// <summary>
    /// Registers a keyboard shortcut for the specified application.
    /// </summary>
    /// <param name="id">The ID of the shortcut to register.</param>
    /// <returns>A <see cref="ShortcutRegistrationResult"/> indicating the result of the registration.</returns>
    public ShortcutRegistrationResult Register(ShortcutId id);

    /// <summary>
    /// Checks if a keyboard shortcut is registered for the specified application.
    /// </summary>
    /// <param name="id">The ID of the shortcut to check.</param>
    /// <returns><c>true</c> if the shortcut is registered; otherwise, <c>false</c>.</returns>
    public bool IsRegistered(ShortcutId id);

    /// <summary>
    /// Unregisters a keyboard shortcut for the specified application.
    /// </summary>
    /// <param name="id">The ID of the shortcut to unregister.</param>
    public void Unregister(ShortcutId id);

    /// <summary>
    /// Unregisters all keyboard shortcuts for the specified application.
    /// </summary>
    /// <param name="appId">The ID of the application.</param>
    public void UnregisterAll(string appId);
}