using System.Windows;

namespace DockManagerCore
{
    public class WindowUnLockButton : WindowButton
    {
        static WindowUnLockButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowUnLockButton),
                new FrameworkPropertyMetadata(typeof(WindowUnLockButton)));
        }
         

        public WindowUnLockButton()
        {
            Command = PaneContainerCommands.Unlock;
        }
    }
}
