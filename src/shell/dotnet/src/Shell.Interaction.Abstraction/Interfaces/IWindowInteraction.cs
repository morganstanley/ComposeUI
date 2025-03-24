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
    void Show(string windowId);

    /// <summary>
    /// Hides the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to hide.</param>
    void Hide(string windowId);

    /// <summary>
    /// Closes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to close.</param>
    void Close(string windowId);

    /// <summary>
    /// Gets the bounds of the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns>A <see cref="Rectangle"/> representing the bounds of the window.</returns>
    Rectangle GetBounds(string windowId);

    /// <summary>
    /// Sets the bounds of the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <param name="bounds">A <see cref="Rectangle"/> representing the new bounds of the window.</param>
    void SetBounds(string windowId, Rectangle bounds);

    /// <summary>
    /// Maximizes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to maximize.</param>
    void Maximize(string windowId);

    /// <summary>
    /// Minimizes the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to minimize.</param>
    void Minimize(string windowId);

    /// <summary>
    /// Restores the window with the specified ID to its previous state.
    /// </summary>
    /// <param name="windowId">The ID of the window to restore.</param>
    void Restore(string windowId);

    /// <summary>
    /// Determines whether the window with the specified ID is currently showing.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns><c>true</c> if the window is showing; otherwise, <c>false</c>.</returns>
    bool IsShowing(string windowId);

    /// <summary>
    /// Brings the window with the specified ID to the front.
    /// </summary>
    /// <param name="windowId">The ID of the window to bring to the front.</param>
    void BringToFront(string windowId);

    /// <summary>
    /// Gets the options for the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns>A <see cref="WindowInteractionOptions"/> object representing the options for the window.</returns>
    WindowInteractionOptions GetOptions(string windowId);

    /// <summary>
    /// Gets the state of the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <returns>A <see cref="WindowInteractionState"/> object representing the state of the window.</returns>
    WindowInteractionState GetState(string windowId);

    /// <summary>
    /// Sets the state of the window with the specified ID.
    /// </summary>
    /// <param name="windowId">The ID of the window.</param>
    /// <param name="state">A <see cref="WindowInteractionState"/> object representing the new state of the window.</param>
    void SetState(string windowId, WindowInteractionState state);
}
