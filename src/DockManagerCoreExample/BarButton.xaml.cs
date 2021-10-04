using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DockManagerCoreExample
{
    /// <summary>
    /// Interaction logic for WindowButton.xaml
    /// </summary>
    public partial class BarButton : Window
    {
        private SelectorWindow win;

        public BarButton(Window w)
        {
            InitializeComponent();
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            win = (SelectorWindow) w;
            Top = 300;
        }


        protected override void OnMouseEnter(MouseEventArgs e)
        {
                Top = win.Height;
                win.Show();
                
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!win.IsMouseOver)
            {
                win.Hide();
                Top = 300;
                Show();
            }
        }

    }
}
