using System.Windows;

namespace DockManagerCore
{
    public class WindowPlainButton : WindowButton
    {
        static WindowPlainButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
                typeof (WindowPlainButton),
                new FrameworkPropertyMetadata(typeof (WindowPlainButton)));
        } 
         
        public DockLocation PlacementLocation
        {
            get => (DockLocation)GetValue(PlacementLocationProperty);
            set => SetValue(PlacementLocationProperty, value);
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlacementLocationProperty =
            DependencyProperty.Register("PlacementLocation", typeof(DockLocation), typeof(WindowPlainButton), new PropertyMetadata(DockLocation.None));


    }

    public enum DockLocation
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        None
    }
}
