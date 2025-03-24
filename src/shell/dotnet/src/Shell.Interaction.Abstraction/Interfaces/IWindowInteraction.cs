using MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Interfaces;

public interface IWindowInteraction
{
    // Base requirements
    void Show(string windowId);

    void Hide(string windowId);

    void Close(string windowId);

    Rectangle GetBounds(string windowId);

    void SetBounds(string windowId, Rectangle bounds);

    void Maximize(string windowId);

    void Minimize(string windowId);

    void Restore(string windowId);

    bool IsShowing(string windowId);

    void BringToFront(string windowId);

    WindowInteractionOptions GetOptions(string windowId);

    WindowInteractionState GetState(string windowId);

    void SetState(string windowId, WindowInteractionState state);

    // Supporting methods
}
