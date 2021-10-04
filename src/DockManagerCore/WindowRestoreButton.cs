using System.Windows;

namespace DockManagerCore
{
    public class WindowRestoreButton : WindowButton
    { 
        static WindowRestoreButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WindowRestoreButton),
        new FrameworkPropertyMetadata(typeof(WindowRestoreButton)));
        }

        public WindowRestoreButton()
        {
            Command = PaneContainerCommands.Restore;
        }
    }
}
