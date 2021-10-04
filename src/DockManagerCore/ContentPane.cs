using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DockManagerCore.Desktop;
using DockManagerCore.Services;
using DockManagerCore.Utilities;

namespace DockManagerCore
{
    public class ContentPane : ContentControl, IDisposable
    {
        public override string ToString()
        { 
            string children = string.Empty;
            if (Wrapper != null && Wrapper.DockingGrid != null)
            {
                foreach (var normalContainer in Wrapper.DockingGrid.NormalContainers)
                {
                    children += normalContainer + ",";
                }
                foreach (var minimized in Wrapper.DockingGrid.MinimizedPaneContainers.Containers)
                {
                    children += minimized + ",";
                }
            }
            if (!string.IsNullOrEmpty(children))
            {
                return Caption + "(" + children.Substring(0, children.Length - 1) + ")";
            }
            return Caption;
        } 

        public LayoutGrid Layouter { get; private set; }
         
        public ContentPane()
        {
            foreach (var command in ContentPaneCommands.GetAllCommands())
            {
                CommandBindings.Add(new CommandBinding(command, ExecuteCommand, CanExecuteCommand));
            } 

            SizeToContent = SizingMethod.SizeToContent; 
            CustomItems = new HeaderItemsCollection(); 
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            ParentContainer.ActivePane = this;
            base.OnGotFocus(e);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ParentContainer.ActivePane = this;
            base.OnGotKeyboardFocus(e);
        }
        
 
        internal ContentPane(DockingGrid dockingGrid_)
            : this()
        {
            Layouter = new LayoutGrid(dockingGrid_);
            Content = Layouter;
            Caption = "Window";
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            FrameworkElement element = newContent as FrameworkElement;
            if (element != null)
            {
                if (SizeToContent == SizingMethod.SizeToContent)
                { 
                    PaneHeight = element.Height;
                    PaneWidth = element.Width;
                    element.ClearValue(HeightProperty);
                    element.ClearValue(WidthProperty); 
                }
            }  
            base.OnContentChanged(oldContent, newContent);
        } 
         
        public DateTime LastActivatedTime { get; set; }

        public Size LastFloatingSize { get; set; } 
        public double PaneWidth
        {
            get => (double)GetValue(PaneWidthProperty);
            set => SetValue(PaneWidthProperty, value);
        }

        // Using a DependencyProperty as the backing store for PaneWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PaneWidthProperty =
            DependencyProperty.Register("PaneWidth", typeof(double), typeof(ContentPane), new PropertyMetadata(double.NaN));
         
        public double PaneHeight
        {
            get => (double)GetValue(PaneHeightProperty);
            set => SetValue(PaneHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for PaneHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PaneHeightProperty =
            DependencyProperty.Register("PaneHeight", typeof(double), typeof(ContentPane), new PropertyMetadata(double.NaN)); 
 
        public bool Renaming
        {
            get => (bool)GetValue(RenamingProperty);
            set => SetValue(RenamingProperty, value);
        }

        // Using a DependencyProperty as the backing store for Renaming.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RenamingProperty =
            DependencyProperty.Register("Renaming", typeof(bool), typeof(ContentPane), new PropertyMetadata(false));


        //todo implement
        private void CanExecuteCommand(object sender_, CanExecuteRoutedEventArgs canExecuteRoutedEventArgs_)
        {
            canExecuteRoutedEventArgs_.CanExecute = true;
            canExecuteRoutedEventArgs_.Handled = true;
        }

        public void ExecuteCommand(RoutedCommand command_)
        {
            if (command_ == ContentPaneCommands.Close)
            {
                Close();
            }
            else if (command_ == ContentPaneCommands.Rename)
            {
                Activate();
                Renaming = true;
            }
            else if (command_ == ContentPaneCommands.Activate)
            {
                Activate();
            }
            else if (command_ == ContentPaneCommands.TearOff)
            {
                TearOff();
            }
           
        }

        private void ExecuteCommand(object sender_, ExecutedRoutedEventArgs executedRoutedEventArgs_)
        {
            ExecuteCommand(executedRoutedEventArgs_.Command as RoutedCommand);
        }

        public ContentPaneWrapper Wrapper
        {
            get => (ContentPaneWrapper)GetValue(WrapperProperty);
            internal set => SetValue(WrapperPropertyKey, value);
        }

        private static readonly DependencyPropertyKey WrapperPropertyKey
        = DependencyProperty.RegisterReadOnly("Wrapper", typeof(ContentPaneWrapper), typeof(ContentPane), new PropertyMetadata(null));

        public static readonly DependencyProperty WrapperProperty
            = WrapperPropertyKey.DependencyProperty;
         
        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        // Using a DependencyProperty as the backing store for Caption.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(ContentPane), new PropertyMetadata(null));

         
        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(ContentPane), new PropertyMetadata(null));
         
        public IList<object> CustomItems
        {
            get => (IList<object>)GetValue(CustomItemsProperty);
            internal set => SetValue(CustomItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey CustomItemsPropertyKey
        = DependencyProperty.RegisterReadOnly("CustomItems", typeof(IList<object>), typeof(ContentPane), new PropertyMetadata(null));

        public static readonly DependencyProperty CustomItemsProperty
            = CustomItemsPropertyKey.DependencyProperty;

        public SizingMethod SizeToContent { get; set; }
        public bool Close()
        {
            if (Wrapper == null || Wrapper.Host == null) return true;
            return Wrapper.Host.Close(Wrapper, false);
        }

        public void Activate()
        {
            ActivateInternal(true);

        } 

        public PaneContainer TearOff()
        {
            var floatingWindow = TearOffInternal();
            return floatingWindow != null ? floatingWindow.PaneContainer : null;
        }

        internal FloatingWindow TearOffInternal() 
        {
            var parentContainer = ParentContainer;
            if (parentContainer == null) return null;
            if (Wrapper.IsSingleTab && parentContainer.IsRootContainer) return null;
            MinimizedPaneContainer minimizeProxy = parentContainer.MinimizedProxy; 
            var floatingPosition = WPFHelper.GetPositionWithOffset(Wrapper.IsSingleTab ? Wrapper.Host: Wrapper);
            PaneContainer paneContainer = null;
            if (Wrapper.IsSingleTab) //tear off a single pane, we could utilize the original PaneContainer
            {
                paneContainer = parentContainer;
                var parentDockingGrid = ParentDockingGrid;
                if (minimizeProxy != null)
                {
                    minimizeProxy.RemoveFromParent();
                }
                else if (parentDockingGrid != null)
                {
                    parentDockingGrid.Remove(parentContainer);
                } 
            }
            else //create a new PaneContainer to host the tab
            {  
                paneContainer = new PaneContainer(); 
                paneContainer.MergePane(this);
            }
            FloatingWindow fw = new FloatingWindow(paneContainer);
            if (LastFloatingSize.Width > 0 && LastFloatingSize.Height > 0)
            {
                paneContainer.PaneHeight = LastFloatingSize.Height;
                paneContainer.PaneWidth = LastFloatingSize.Width;
            }
            else
            {
                paneContainer.PaneHeight = parentContainer.PaneHeight;
                paneContainer.PaneWidth = parentContainer.PaneWidth;
            }
            fw.Top = floatingPosition.Y;
            fw.Left = floatingPosition.X;
            DockService.SetLastPosition(fw, WPFHelper.GetMousePosition());
            fw.Show(); 
            return fw;
        }

        public void Detach()
        {
            var parentContainer = ParentContainer;
            var parentDockingGrid = ParentDockingGrid;
            if (parentContainer == null) return; 
            MinimizedPaneContainer minimizeProxy = parentContainer.MinimizedProxy;
            Wrapper.Host.RemovePaneWrapper(Wrapper); //remove from the host container
            if (minimizeProxy != null)
            {
                minimizeProxy.RemoveFromParent();
            }
            else if (parentDockingGrid != null)
            {
                parentDockingGrid.Remove(parentContainer);
            }
            Wrapper.DockingGrid.Content = null;
            Wrapper = null;
        } 

        public PaneContainer ParentContainer
        {
            get 
            { 
                if (Wrapper == null) return null;
                if (Wrapper.Host == null) return null;
                return Wrapper.Host.ParentContainer;
            }
        }

        internal DockingGrid ParentDockingGrid
        {
            get
            {
                var parentContainer = ParentContainer;
                if (parentContainer == null) return null;
                var parentPane = parentContainer.FindVisualParent<ContentPane>();
                if (parentPane == null) return null;
                return parentPane.Wrapper.DockingGrid;
            }
        }


        internal void OnClosing(ClosingEventArgs arg_)
        {
            var copy = Closing;
            if (copy != null)
            {
                copy(this, arg_);
            }
        }

        internal void OnClosed()
        {
            var copy = Closed;
            if (copy != null)
            {
                copy(this, EventArgs.Empty);
            }
        }
        public event EventHandler Closed;
        public event EventHandler<ClosingEventArgs> Closing;

        internal bool CanActivate => ((Visibility == Visibility.Visible) && IsEnabled);

        internal bool ActivateInternal(bool bringIntoView_)
        {
            if (!CanActivate) return false;
            if (bringIntoView_)
            {
                var thisContainer = ParentContainer;
                var thisWrapper = Wrapper;
                while (thisContainer != null)
                {
                    if (thisContainer.WindowState == WindowState.Minimized)
                    {
                        thisContainer.ExecuteCommand(PaneContainerCommands.Restore);
                    }
                    if (thisWrapper != null)
                    {
                        if (thisContainer.Host.SelectedItem != thisWrapper)
                        {
                            thisContainer.Host.SelectedItem = thisWrapper;
                        }
                        thisWrapper = thisContainer.FindVisualParent<ContentPaneWrapper>();
                    }
                    thisContainer = thisContainer.FindVisualParent<PaneContainer>();
                } 
            }
             
            if (!bringIntoView_)
            {
                Focus();
                ActivateCurrentPane();
                return true;
            }
            FloatingWindow window = FloatingWindow.GetFloatingWindow(this);
            if (window == null) return false;
             
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            if (!window.Activate())
            {
                return false;
            }
            if (window.WindowState == WindowState.Normal)
            {
                window.BringIntoView();
            }
            BringIntoView();
            Focus();
            if (IsVisible && IsFocused)
            {
                ActivateCurrentPane();
                return true;
            }
            return false;
           
        }

        private void ActivateCurrentPane()
        {
            DockManager.ActiveContainer = ParentContainer;
            Wrapper.Host.ActiveWrapper = Wrapper;
            ParentContainer.ActivePane = this; 
        } 
        public void Dispose()
        {
            //todo: implement it
             
        }

        internal static readonly IComparer<ContentPane> Comparer = new ActivatedTimeComparer();

        private class ActivatedTimeComparer : IComparer<ContentPane>
        {
            public int Compare(ContentPane x_, ContentPane y_)
            {
                return x_.LastActivatedTime.CompareTo(y_.LastActivatedTime);
            }
        }
    }

    public class ClosingEventArgs:CancelEventArgs
    {
        public ClosingEventArgs(bool emptyContent_)
        {
            EmptyContent = emptyContent_;
        }

        public bool EmptyContent { get; private set; }
    }

}
