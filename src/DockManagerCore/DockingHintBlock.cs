using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore
{
    public class DockingHintBlock:Control
    {
        static DockingHintBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DockingHintBlock),
                new FrameworkPropertyMetadata(typeof(DockingHintBlock)));
        }
        public DockLocation Dock
        {
            get => (DockLocation)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register("Dock", typeof(DockLocation), typeof(DockingHintBlock), new PropertyMetadata(DockLocation.None, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {

        }
    }
}
