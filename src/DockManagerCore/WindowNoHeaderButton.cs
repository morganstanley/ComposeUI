using System.Windows;

namespace DockManagerCore
{
    public class WindowNoHeaderButton : WindowButton
    {
     
        static WindowNoHeaderButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
        typeof(WindowNoHeaderButton),
        new FrameworkPropertyMetadata(typeof(WindowNoHeaderButton)));
        }

        public WindowNoHeaderButton()
        {
            Command = PaneContainerCommands.HideHeader;
        }
    }
}
