using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DockManagerCore.Desktop
{
    public abstract class PaneFactory : ContainerFactory
    {
        internal static event RoutedPropertyChangedEventHandler<UIElement> ActivePaneChanged;
        protected static void HandleActivePaneChanged<TContentPane>(object sender_, TContentPane oldValue_, TContentPane newValue_) where TContentPane : FrameworkElement
        {
            if (oldValue_ != null && oldValue_.DataContext is WindowViewModel)
            {
                if (LinkControlFocusHackEnabled &&
                    (oldValue_.DataContext as WindowViewModel).HeaderItems.FirstOrDefault(o => (o is Image) && (o as Image).Tag.ToString() == "LinkControl") != null)
                {
                    #region LinkControlFocusHack

                    if (FocusBounceBackNeeded && BounceBackTo == null)
                        BounceBackTo = oldValue_;

                    if (FocusBounceBackNeeded && oldValue_ == BounceBackTo && newValue_ != null)
                    {
                        var vm = ((WindowViewModel)newValue_.DataContext);
                        PropertyChangedEventHandler hack = null;
                        hack = (a, b) =>
                        {
                            if (b.PropertyName != "IsActive") return;
                            /*ContentPaneFactory.FocusBounceBackNeeded = false; e_.OldValue.Activate();*/
                            oldValue_.Focus();
                            vm.PropertyChanged -= hack;
                        };
                        vm.PropertyChanged += hack;
                        ((WindowViewModel)oldValue_.DataContext).IsActive = false;
                    }
                    else
                    {
                        //var focusedChild = FindFocusedChild(e_.OldValue);

                        var holderItemClicked = false;
                        var m_window = Window.GetWindow(oldValue_);
                        var clickedPoint = Mouse.GetPosition(m_window);

                        if (m_window != null && clickedPoint.X >= 0 && clickedPoint.Y >= 0)
                        {
                            VisualTreeHelper.HitTest
                                (
                                    m_window,
                                    null,
                                    result_ =>
                                    {
                                        var visualHit = result_.VisualHit;
                                        var holder = GetVisualParent<HeaderItemsHolder>(visualHit); // object is in the holder
                                        if (holder != null && GetVisualParent<TContentPane>(holder) == newValue_)
                                        {
                                            holderItemClicked = true;
                                        }
                                        return HitTestResultBehavior.Continue;
                                    },
                                    new PointHitTestParameters(clickedPoint)
                                );

                            if ((holderItemClicked) && (newValue_ != null))
                            {
                                var vm = ((WindowViewModel)newValue_.DataContext);
                                PropertyChangedEventHandler hack = null;
                                hack = (a, b) => { /*e_.OldValue.Activate();*/ oldValue_.Focus(); vm.PropertyChanged -= hack; };
                                vm.PropertyChanged += hack;
                            }
                            else
                            {
                                ((WindowViewModel)oldValue_.DataContext).IsActive = false;
                            }
                        }
                    }

                    #endregion LinkControlFocusHack
                }
                else
                {
                    ((WindowViewModel)oldValue_.DataContext).IsActive = true;
                }
            }
            if (newValue_ != null && newValue_.DataContext is WindowViewModel)
            {
                ((WindowViewModel)newValue_.DataContext).IsActive = true;
            }
            var copy = ActivePaneChanged;
            if (copy != null)
            {
                copy(sender_, new RoutedPropertyChangedEventArgs<UIElement>(oldValue_, newValue_));
            }
        }

        public static T GetVisualParent<T>(object childObject_) where T : Visual
        {
            DependencyObject child = childObject_ as DependencyObject;
            while ((child != null) && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }

        public static readonly DependencyProperty ClosingWorkSpaceProperty = DependencyProperty.Register("ClosingWorkSpace",
            typeof(ICommand), typeof(PaneFactory), new FrameworkPropertyMetadata(null));

        public ICommand ClosingWorkSpace
        {
            get => (ICommand)GetValue(ClosingWorkSpaceProperty);
            set => SetValue(ClosingWorkSpaceProperty, value);
        }

        public static readonly DependencyProperty ClosedWorkSpaceProperty = DependencyProperty.Register("ClosedWorkSpace",
             typeof(ICommand), typeof(PaneFactory), new FrameworkPropertyMetadata(null));

        public ICommand ClosedWorkSpace
        {
            get => (ICommand)GetValue(ClosedWorkSpaceProperty);
            set => SetValue(ClosedWorkSpaceProperty, value);
        }
        /// <summary>
        /// Identifies the <see cref="RemoveItemOnClose"/> dependency property
        /// </summary>
        public static readonly DependencyProperty RemoveItemOnCloseProperty = DependencyProperty.Register("RemoveItemOnClose",
            typeof(bool), typeof(PaneFactory), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Returns or sets a boolean indicating whether to remove the item when the pane was closed.
        /// </summary>
        /// <seealso cref="RemoveItemOnCloseProperty"/>
        [Description("Returns or sets a boolean indicating whether to remove the item when the pane was closed.")]
        [Category("Behavior")]
        [Bindable(true)]
        public bool RemoveItemOnClose
        {
            get => (bool)GetValue(RemoveItemOnCloseProperty);
            set => SetValue(RemoveItemOnCloseProperty, value);
        }


        public static readonly DependencyProperty EnforceSizeRestrictionsProperty = DependencyProperty.Register("EnforceSizeRestrictions",
            typeof(bool), typeof(PaneFactory), new FrameworkPropertyMetadata(true));

        public bool EnforceSizeRestrictions
        {
            get => (bool)GetValue(EnforceSizeRestrictionsProperty);
            set => SetValue(EnforceSizeRestrictionsProperty, value);
        }

        private void SizeRestrictionHandler(object sender, EventArgs args)
        {
            WindowInteropHelper windowInteropHelper = null;
            Screen screen = null;
            var tw = (FrameworkElement)sender;
            var window = Window.GetWindow(tw);
            if (window == null)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    window = Window.GetWindow(tw);
                    if (window == null) return;
                    windowInteropHelper = new WindowInteropHelper(window);
                    screen = Screen.FromHandle(windowInteropHelper.Handle);
                    tw.MaxWidth = screen.WorkingArea.Width;
                    tw.MaxHeight = screen.WorkingArea.Height;
                }), DispatcherPriority.ApplicationIdle);
                return;
            }
            windowInteropHelper = new WindowInteropHelper(window);
            screen = Screen.FromHandle(windowInteropHelper.Handle);
            tw.MaxWidth = screen.WorkingArea.Width;
            tw.MaxHeight = screen.WorkingArea.Height;
        }


        protected void AddSizeRestrictionsHandler(FrameworkElement window)
        {
            DependencyPropertyDescriptor.FromProperty(Window.TopProperty, window.GetType()).AddValueChanged(
                                window, SizeRestrictionHandler);
            DependencyPropertyDescriptor.FromProperty(Window.LeftProperty, window.GetType()).AddValueChanged(
                window, SizeRestrictionHandler);
        }

        protected void RemoveSizeRestrictionsHandler(FrameworkElement window)
        {

            DependencyPropertyDescriptor.FromProperty(Window.TopProperty, window.GetType()).RemoveValueChanged(
                                window, SizeRestrictionHandler);
            DependencyPropertyDescriptor.FromProperty(Window.LeftProperty, window.GetType()).RemoveValueChanged(
                window, SizeRestrictionHandler);
        }

        internal static bool FocusBounceBackNeeded = false;
        internal static bool LinkControlFocusHackEnabled = false;
        internal static object BounceBackTo;
    }
}
