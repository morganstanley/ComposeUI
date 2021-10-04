using System.Windows;

namespace DockManagerCore
{
    public class WindowCloseButton : WindowButton
    {

        static WindowCloseButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowCloseButton),
                new FrameworkPropertyMetadata(typeof(WindowCloseButton)));
        }

        public WindowCloseButton()
        {
            Command = PaneContainerCommands.Close;
        }
    }
}
