using System.Windows;

namespace DockManagerCore
{
    public class WindowHeaderButton : WindowButton
    {

        static WindowHeaderButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
                typeof (WindowHeaderButton),
                new FrameworkPropertyMetadata(typeof (WindowHeaderButton)));
        }

        public WindowHeaderButton()
        {
            Command = PaneContainerCommands.ShowHeader;
        }
    }
}
