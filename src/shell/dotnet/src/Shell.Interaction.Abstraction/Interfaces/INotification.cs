using MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

/// <summary>
/// Defines the interface for showing notifications.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Shows a notification with the specified window ID, title, and options.
    /// </summary>
    /// <param name="windowId">The ID of the window where the notification will be shown.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="options">The options for the notification, such as URL and body content.</param>
    void ShowNotification(string windowId, string title, NotificationOptions options);
}
