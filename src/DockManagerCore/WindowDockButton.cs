using System.Windows;

namespace DockManagerCore
{
    public class WindowDockButton : WindowButton
    {

        static WindowDockButton()
        { 
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof (WindowDockButton),
                new FrameworkPropertyMetadata(typeof (WindowDockButton)));
        }
         
    }
}
