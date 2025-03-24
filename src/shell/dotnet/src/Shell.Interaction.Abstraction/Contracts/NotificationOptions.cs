namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

/// <summary>
/// Represents the options for a notification, including the URL and body content.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the URL associated with the notification.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the body content of the notification.
    /// </summary>
    public string? Body { get; set; }
}
