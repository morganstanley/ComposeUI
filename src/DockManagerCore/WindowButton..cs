/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */
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
