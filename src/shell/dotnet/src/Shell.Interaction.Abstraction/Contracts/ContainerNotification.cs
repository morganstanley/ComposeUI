namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

/// <summary>
/// Represents a notification for a container with event handlers for click and error events.
/// </summary>
public class ContainerNotification
{
    /// <summary>
    /// Gets or sets the event handler for the click event.
    /// </summary>
    public object? OnClick { get; set; }

    /// <summary>
    /// Gets or sets the event handler for the error event.
    /// </summary>
    public object? OnError { get; set; }
}
