using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DockManagerCore
{
    public class WindowButton : Button
    {
        #region DependencyProperties

        /// <summary>
        /// Button content
        /// <remarks>Base button's content property is hidden</remarks>
        /// </summary>
        public object ContentEnabled
        {
            get => GetValue(ContentEnabledProperty);
            set => SetValue(ContentEnabledProperty, value);
        }

        public static readonly DependencyProperty ContentEnabledProperty =
            DependencyProperty.Register("ContentEnabled", typeof(object), typeof(WindowButton), new UIPropertyMetadata(null, ContentEnableChangedCallback));

        private static void ContentEnableChangedCallback(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            WindowButton button = (WindowButton)dependencyObject_;
            if (button.ContentEnabled != null && button.ContentDisabled == null)
            {
                button.ContentDisabled = button.ContentEnabled;
            }
        }

        /// <summary>
        /// Disabled button content
        /// </summary>
        public object ContentDisabled
        {
            get => GetValue(ContentDisabledProperty);
            set => SetValue(ContentDisabledProperty, value);
        }

        public static readonly DependencyProperty ContentDisabledProperty =
            DependencyProperty.Register("ContentDisabled", typeof(object), typeof(WindowButton), new UIPropertyMetadata());

        /// <summary>
        /// Corner radius of the button
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(WindowButton), new UIPropertyMetadata(new CornerRadius()));


        #endregion

        /// <summary>
        /// Button default Background 
        /// </summary>
        public virtual Brush BackgroundDefaultValue => (Brush)FindResource("DefaultBackgroundBrush");

        static WindowButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowButton),
                new FrameworkPropertyMetadata(typeof(WindowButton)));
        }
         
  

        public WindowButtonState State
        {
            get => (WindowButtonState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        

        // Using a DependencyProperty as the backing store for State.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(WindowButtonState), typeof(WindowButton), new PropertyMetadata(WindowButtonState.Normal, StatePropertyChangedCallback));

        private static void StatePropertyChangedCallback(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            WindowButton button = (WindowButton)dependencyObject_;
            switch ((WindowButtonState)dependencyPropertyChangedEventArgs_.NewValue)
            {
                case WindowButtonState.Normal:
                    button.Visibility = Visibility.Visible;
                    button.IsEnabled = true;
                    break;

                case WindowButtonState.Disabled:
                    button.Visibility = Visibility.Visible;
                    button.IsEnabled = false;
                    break;

                case WindowButtonState.None:
                    button.Visibility = Visibility.Collapsed;
                    break;
            }
        }

    }
}
