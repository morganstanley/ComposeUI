using System.Windows;

namespace DockManagerCore
{
    public class WindowMaximizeButton : WindowButton
    { 
      static WindowMaximizeButton()
      {

          DefaultStyleKeyProperty.OverrideMetadata(
      typeof(WindowMaximizeButton),
      new FrameworkPropertyMetadata(typeof(WindowMaximizeButton)));
      }

        public WindowMaximizeButton()
        {
            Command = PaneContainerCommands.Maximize;
        }
    }
}
