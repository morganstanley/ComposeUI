using System.Windows;

namespace DockManagerCore
{
    public class WindowMinimizeButton : WindowButton
    { 
        static WindowMinimizeButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WindowMinimizeButton),
        new FrameworkPropertyMetadata(typeof(WindowMinimizeButton)));
        }

        public WindowMinimizeButton()
        {
            Command = PaneContainerCommands.Minimize;
        }
    }
}
