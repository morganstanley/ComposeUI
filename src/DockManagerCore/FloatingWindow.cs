using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using DockManagerCore.Desktop;
using DockManagerCore.Services;

namespace DockManagerCore
{
    internal class FloatingWindow : Window
    {
        private readonly PaneContainer rootPaneContainer;
        // implements resizing window when aero is off
        WindowResizingAdorner resizingAdorner;

        public PaneContainer PaneContainer => rootPaneContainer;

        public FloatingWindow()
        {
            this.Resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/DockManagerCore;component/Themes/Generic.xaml") };
            //AllowsTransparency = true;
            //Background = System.Windows.Media.Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            Loaded += OnLoaded;
            GridServices.Add(this);
            SourceInitialized += FloatingWindow_SourceInitialized;
        }

        void FloatingWindow_SourceInitialized(object sender_, EventArgs e_)
        {
            MaximizeFloatingWindowExtensionAdapter.FixResize(this);
        }

        public FloatingWindow(PaneContainer rootContainer_)
            : this()
        {
            DockManager.ActiveContainer = rootContainer_;
            rootPaneContainer = rootContainer_;
            Content = rootPaneContainer;
            Title = "Window";
            //this.WindowState = p.WindowState;
            rootPaneContainer.ContainerStateChanged += StateChange;
            rootPaneContainer.ContainerCloseRequest += HandleClose;
        }

        public static FloatingWindow GetFloatingWindow(DependencyObject element_)
        {
            return element_.FindVisualParent<FloatingWindow>();
        }

        private bool internalClose;
        internal void CloseInternal()
        {
            internalClose = true;
            Close();
        }
        protected override void OnClosed(EventArgs e_)
        {
            base.OnClosed(e_);
            UnRegister();
            if (!internalClose && Content != null)
            {
                PaneContainer rootContainer = Content as PaneContainer;
                if (rootContainer != null)
                {
                    rootContainer.Close(false);
                }
            }
            Content = null;
            GridServices.Remove(this);
            DockManager.RemoveWindow(this);
        }

        private void UnRegister()
        {
            rootPaneContainer.ContainerStateChanged -= StateChange;
            rootPaneContainer.ContainerCloseRequest -= HandleClose;
        }

        protected void OnLoaded(object sender_, EventArgs e_)
        {
            BindContainer();
            ResizeMode = ResizeMode.NoResize;
            resizingAdorner = new WindowResizingAdorner((UIElement)Content, this);
            resizingAdorner.SetBinding(VisibilityProperty,
                                       new Binding("IsLocked")
                                       {
                                           Source = PaneContainer,
                                           Converter = new ConfigurableBooleanToVisibilityConverter { VisibilityWhenFalse = Visibility.Visible, VisibilityWhenTrue = Visibility.Collapsed },

                                       });
            AdornerLayer.GetAdornerLayer((UIElement)Content).Add(resizingAdorner);
            LastActivatedTime = DateTime.UtcNow;
            StateChanged += WindowStateChange;
            DockManager.AddWindow(this);
        }
 
        private void BindContainer()
        {

            SetBinding(MaxWidthProperty, new Binding("MaxWidth") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            SetBinding(MinWidthProperty, new Binding("MinWidth") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            SetBinding(MaxHeightProperty, new Binding("MaxHeight") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            SetBinding(MinHeightProperty, new Binding("MinHeight") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            SetBinding(VisibilityProperty, new Binding("Visibility") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            SetBinding(ShowInTaskbarProperty, new Binding("ShowInTaskbar") { Source = rootPaneContainer, Mode = BindingMode.OneWay });
            rootPaneContainer.SuspendChangeSize();
            if (WindowState == WindowState.Normal)
            {
                if (double.IsNaN(rootPaneContainer.PaneWidth) && double.IsNaN(rootPaneContainer.PaneHeight))
                {
                    SizeToContent = SizeToContent.WidthAndHeight;
                    double width = ActualWidth;
                    double height = ActualHeight;
                    SizeToContent = SizeToContent.Manual;
                    Width = width;
                    Height = height;
                }
                else if (double.IsNaN(rootPaneContainer.PaneWidth))
                {
                    SizeToContent = SizeToContent.Width;
                    double width = ActualWidth;
                    SizeToContent = SizeToContent.Manual;
                    Width = width;
                }
                else if (double.IsNaN(rootPaneContainer.PaneHeight))
                {
                    SizeToContent = SizeToContent.Height;
                    double height = ActualHeight;
                    SizeToContent = SizeToContent.Manual;
                    Height = height;
                }
                else
                {
                    Width = rootPaneContainer.PaneWidth;
                    Height = rootPaneContainer.PaneHeight;
                }
            }
            rootPaneContainer.Width = Width;
            rootPaneContainer.Height = Height;
            rootPaneContainer.PerformChangeSize();
            //SetBinding(WidthProperty, new Binding("Width") { Source = rootPaneContainer, Mode = BindingMode.TwoWay });
            //SetBinding(HeightProperty, new Binding("Height") { Source = rootPaneContainer, Mode = BindingMode.TwoWay });
            SizeChanged += FloatingWindow_SizeChanged;
        }

        private void FloatingWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (syncingSize || Content == null) return;
            rootPaneContainer.InternalSyncSize(e.WidthChanged, e.HeightChanged);
        }

        private bool syncingSize;
        internal void InternalSyncSize(bool updateWidth_, bool updateHeight_)
        {
            if (syncingSize) return;
            syncingSize = true;
            if (updateWidth_)
            { 
                Width = rootPaneContainer.Width;

            }
            if (updateHeight_)
            { 
                Height = rootPaneContainer.Height;
            } 
            syncingSize = false;
        }
        public DateTime LastActivatedTime { get; set; }

        protected void WindowStateChange(object sender_, EventArgs e_)
        {
            rootPaneContainer.WindowState = WindowState;
            if (WindowState == WindowState.Minimized)
            {
                DockManager.ActiveContainer = null;
            }
        }

        protected void StateChange(object sender_, EventArgs e_)
        {
            PaneContainer paneContainer = (PaneContainer)sender_;
            WindowState = paneContainer.WindowState;
        }

        protected void HandleClose(object sender_, HandledEventArgs e_)
        {
            CloseInternal();
            e_.Handled = true;
        }

        internal static readonly IComparer<FloatingWindow> Comparer = new ActivatedTimeComparer();

        private class ActivatedTimeComparer : IComparer<FloatingWindow>
        {
            public int Compare(FloatingWindow x_, FloatingWindow y_)
            {
                return x_.LastActivatedTime.CompareTo(y_.LastActivatedTime);
            }
        }

 
      

    }
}
