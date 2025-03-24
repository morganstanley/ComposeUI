using MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

public interface INotification
{
    void ShowNotification(string windowId, string title, NotificationOptions options);
}
