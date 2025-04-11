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
/// Defines the interface for window interaction, including methods for showing, hiding, closing, and manipulating window states and bounds.
/// </summary>
public interface IWindowInteraction
{
    /// <summary>
    /// Shows the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to show.</param>
    public void Show(string windowId);

    /// <summary>
    /// Hides the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to hide.</param>
    public void Hide(string windowId);

    /// <summary>
    /// Closes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to close.</param>
    public void Close(string windowId);

    /// <summary>
    /// Gets the bounds of the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns>A <see cref="Rectangle"/> representing the bounds of the window.</returns>
    public Rectangle GetBounds(string windowId);

    /// <summary>
    /// Sets the bounds of the window with the specified ID.
    /// </summary>
    /// <param name="request"> The request containing the window ID and the new bounds.</param>
    public void SetBounds(SetBoundsRequest request);

    /// <summary>
    /// Maximizes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to maximize.</param>
    public void Maximize(string windowId);

    /// <summary>
    /// Minimizes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to minimize.</param>
    public void Minimize(string windowId);

    /// <summary>
    /// Restores the window with the specified ID to its previous state.
    /// </summary>
    /// <param name="windowId">The ID of the window to restore.</param>
    public void Restore(string windowId);

    /// <summary>
    /// Determines whether the window with the specified ID is currently showing.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns><c>true</c> if the window is showing; otherwise, <c>false</c>.</returns>
    public bool IsShowing(string windowId);

    /// <summary>
    /// Brings the window with the specified ID to the front.
    /// </summary>
    /// <param name="windowId">The ID of the window to bring to the front.</param>
    public void BringToFront(string windowId);
}