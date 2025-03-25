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