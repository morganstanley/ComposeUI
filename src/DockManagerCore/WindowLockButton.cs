using System.Windows;

namespace DockManagerCore
{
    public class WindowLockButton : WindowButton
    {
         
        static WindowLockButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WindowLockButton),
        new FrameworkPropertyMetadata(typeof(WindowLockButton)));
        } 

        public WindowLockButton()
        {
            Command = PaneContainerCommands.Lock;
        }
        

    }
}
