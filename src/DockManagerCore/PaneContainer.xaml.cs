using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DockManagerCore.Desktop;
using DockManagerCore.Services;
using DockManagerCore.Utilities;

namespace DockManagerCore
{
    //[TemplatePart(Name = "PART_Header", Type = typeof(PaneContainerHeaderControl))]
    //[TemplatePart(Name = "PART_CaptionBar", Type = typeof(Border))]
    //[TemplatePart(Name = "PART_WrapperHost", Type = typeof(ContentPaneWrapperHost))]
    public partial class PaneContainer : UserControl, IPaneContainer, IDisposable
    {
        public event EventHandler ContainerStateChanged;
        public event EventHandler<HandledEventArgs> ContainerCloseRequest;
        public event EventHandler ContainerIsLockedChanged;
        public event EventHandler ContainerHeaderVisibleChanged;

        public event MouseButtonEventHandler TitleBarClick;
        public event MouseButtonEventHandler TitleBarUnClick;
        public event MouseEventHandler ContainerDragMove;

        public const string DefaultName = "Container";

        ContentPane currentMovingPane;
        public override string ToString()
        {
            string result = string.Empty;
            foreach (var pane in GetChildrenPanes())
            {
                result += pane + ","; 
            }

            return string.IsNullOrEmpty(result) ? result : result.Substring(0, result.Length - 1);
        }


        private bool syncingSize;
        internal void InternalSyncSize(bool updateWidth_, bool upateHeight_)
        {
            if (syncingSize) return;
            syncingSize = true;
            Window w = Window.GetWindow(this);
            if (updateWidth_)
            {
                Width = w.Width;
            }
            if (upateHeight_)
            {
                Height = w.Height;
            }
            syncingSize = false;
        }
        #region Public Properties
         
        public DateTime LastActivatedTime { get; set; }

        public Size LastFloatingSize { get; set; } 
         
        public ContentPane ActivePane
        {
            get => (ContentPane)GetValue(ActivePaneProperty);
            internal set => SetValue(ActivePanePropertyKey, value);
        }

        private static readonly DependencyPropertyKey ActivePanePropertyKey
        = DependencyProperty.RegisterReadOnly("ActivePane", typeof(ContentPane), typeof(PaneContainer), new PropertyMetadata(null));

        public static readonly DependencyProperty ActivePaneProperty
            = ActivePanePropertyKey.DependencyProperty;
        

        internal DockingGrid ActiveGrid
        {
            get
            {
                //if (paneWrapperHost == null) return singlePaneWrapper == null ? null : singlePaneWrapper.DockingGrid;
                var activeWrapper = paneWrapperHost.ActiveWrapper;
                if (activeWrapper != null) return activeWrapper.DockingGrid;
                return null;
            }
        }
        public bool IsMoving
        {
            set;
            get;
        }

        public ContentPaneWrapperHost Host => paneWrapperHost;

        public Double PaneHeight { get; set; }

        public Double PaneWidth { get; set; }

        public WindowState WindowState
        {
            get => (WindowState)GetValue(WindowStateProperty);
            set => SetValue(WindowStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for WindowState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowStateProperty =
            DependencyProperty.Register("WindowState", typeof(WindowState), typeof(PaneContainer), new PropertyMetadata(WindowState.Normal, StateChanged));

        private static void StateChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer pane = (PaneContainer)dependencyObject_;
            var copy = pane.ContainerStateChanged;
            if (copy != null)
            {
                copy(pane, EventArgs.Empty);
            }
        }

        public bool IsLocked
        {
            get => (bool)GetValue(IsLockedProperty);
            set => SetValue(IsLockedProperty, value);
        }

        // Using a DependencyProperty as the backing store for WindowLockState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLockedProperty =
            DependencyProperty.Register("IsLocked", typeof(bool), typeof(PaneContainer), new PropertyMetadata(false, IsLockedChanged));

        private static void IsLockedChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            
            if (!container.IsLocked)
            {
                if (container.WindowState != WindowState.Maximized)
                {
                    container.MaximizeButtonState = WindowButtonState.Normal;
                }
                if (container.WindowState != WindowState.Minimized)
                {
                    container.MinimizeButtonState = WindowButtonState.Normal;
                }  
                container.CloseButtonState = WindowButtonState.Normal;
                
            }
            else
            {
                container.DockButtonState = WindowButtonState.None;
                container.MinimizeButtonState = WindowButtonState.None;
                container.CloseButtonState = WindowButtonState.None; 
                container.MaximizeButtonState = WindowButtonState.None;
                if (DockManager.ActiveContainer == container)
                {
                    DockManager.ExecuteActions(container_ =>  container_.DockButtonState = WindowButtonState.None);
                }

            }
            DockManager.ExecuteActions(container, container_ =>
                {
                    if (container_ != container)
                    {
                        container_.LockButtonState = container.IsLocked
                                                         ? WindowButtonState.None
                                                         : WindowButtonState.Normal;
                        if (!container_.IsHeaderVisible && !container.IsLocked)
                        {
                            container_.headerControl.HideLockButtons();
                            return;
                        }
                    } 
                    container_.IsLocked = container.IsLocked;
                });

            var copy = container.ContainerIsLockedChanged;
            if (copy != null)
            {
                copy(container, EventArgs.Empty);
            }
        }

        public WindowButtonState MinimizeButtonState
        {
            get => (WindowButtonState)GetValue(MinimizeButtonStateProperty);
            set => SetValue(MinimizeButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for MinimizeButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimizeButtonStateProperty =
            DependencyProperty.Register("MinimizeButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal));

        public WindowButtonState MaximizeButtonState
        {
            get => (WindowButtonState)GetValue(MaximizeButtonStateProperty);
            set => SetValue(MaximizeButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for MaximizeButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximizeButtonStateProperty =
            DependencyProperty.Register("MaximizeButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal));


        public WindowButtonState CloseButtonState
        {
            get => (WindowButtonState)GetValue(CloseButtonStateProperty);
            set => SetValue(CloseButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for CloseButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CloseButtonStateProperty =
            DependencyProperty.Register("CloseButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal));

        public WindowButtonState LockButtonState
        {
            get => (WindowButtonState)GetValue(LockButtonStateProperty);
            set => SetValue(LockButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for LockButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LockButtonStateProperty =
            DependencyProperty.Register("LockButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal));


        public WindowButtonState HideButtonState
        {
            get => (WindowButtonState)GetValue(HideButtonStateProperty);
            set => SetValue(HideButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for HideButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideButtonStateProperty =
            DependencyProperty.Register("HideButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal));

        public WindowButtonState DockButtonState
        {
            get => (WindowButtonState)GetValue(DockButtonStateProperty);
            set => SetValue(DockButtonStateProperty, value);
        }

        // Using a DependencyProperty as the backing store for DockButtonState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DockButtonStateProperty =
            DependencyProperty.Register("DockButtonState", typeof(WindowButtonState), typeof(PaneContainer), new PropertyMetadata(WindowButtonState.Normal, DockButtonStateChanged, CoerceDockButtonStateCallback));

        private static object CoerceDockButtonStateCallback(DependencyObject dependencyObject_, object baseValue_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            return container.IsLocked ? WindowButtonState.None : baseValue_;
        }

        private static void DockButtonStateChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            if (container.DockButtonState == WindowButtonState.None)
            {
                container.HideDockButton();
            }
            else
            {
                container.ShowDockButton();
            }
        }


        public bool IsHeaderVisible
        {
            get => (bool)GetValue(IsHeaderVisibleProperty);
            set => SetValue(IsHeaderVisibleProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsHeaderVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsHeaderVisibleProperty =
            DependencyProperty.Register("IsHeaderVisible", typeof(bool), typeof(PaneContainer), new PropertyMetadata(true, IsHeaderVisibleChanged));

        private static void IsHeaderVisibleChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_; 
            DockManager.ExecuteActions(container, container_ =>
            {
                if (container_ != container)
                {
                    container_.HideButtonState = container.IsHeaderVisible
                                                     ? WindowButtonState.Normal
                                                     : WindowButtonState.None;
                }
                container_.IsHeaderVisible = container.IsHeaderVisible;

            });
            var copy = container.ContainerHeaderVisibleChanged;
            if (copy != null)
            {
                copy(container, EventArgs.Empty);
            }
        }
        #endregion

        #region Public Methods
        public void ExecuteCommand(RoutedCommand command_)
        {
            if (command_ == PaneContainerCommands.HideHeader)
            {
                IsHeaderVisible = false;
            }
            else if (command_ == PaneContainerCommands.ShowHeader)
            {
                IsHeaderVisible = true;
            }
            else if (command_ == PaneContainerCommands.Minimize)
            {
                WindowState = WindowState.Minimized;
            }
            else if (command_ == PaneContainerCommands.Restore)
            {
                if (WindowState == WindowState.Minimized && MinimizedProxy != null)
                {
                    MinimizedProxy.Command.Execute(null);
                }
                else if (WindowStateResolved != WindowState.Minimized)
                { 
                    WindowState = WindowState.Normal;
                }
            }
            else if (command_ == PaneContainerCommands.Maximize)
            {
                WindowState = WindowState.Maximized;
            }
            else if (command_ == PaneContainerCommands.Lock)
            {
                IsLocked = true;

            }
            else if (command_ == PaneContainerCommands.Unlock)
            { 
                IsLocked = false; 
            }
            else if (command_ == PaneContainerCommands.Close)
            {
                Close();
            }
            else if (command_ == PaneContainerCommands.TearOff)
            {
                TearOff();
            }
        }

        public void Close()
        {
            Close(false);
        }

        internal bool Close(bool emptyContainer_)
        {
            ClosingEventArgs arg = new ClosingEventArgs(emptyContainer_);
            OnClosing(arg);
            if (arg.Cancel) return false;
            if (!emptyContainer_)
            {
                List<ContentPane> allChildPanes = this.GetAllPanes(true);
                foreach (var childPane in allChildPanes)
                {
                    if (!childPane.Close())
                    {
                        return false;
                    }
                }
            }

            var copy = ContainerCloseRequest;
            if (copy != null)
            {
                var handledArg = new HandledEventArgs(false);
                copy(this, handledArg);
                if (!handledArg.Handled) return false;
            }
            OnClosed();
            Dispose();
            return true;
        }

        public void MergePanes(PaneContainer secondPaneContainer_)
        {
            List<ContentPaneWrapper> paneWrappers = secondPaneContainer_.paneWrapperHost.Items.Cast<ContentPaneWrapper>().ToList();
            foreach (ContentPaneWrapper paneWrapper in paneWrappers)
            {
                secondPaneContainer_.paneWrapperHost.RemovePaneWrapper(paneWrapper);
                paneWrapperHost.AddPaneWrapper(paneWrapper);
            }
        }
          
        public void MergePane(ContentPane pane_)
        {
            if (pane_.Wrapper == null) return;
            if (pane_.Wrapper.Host == null) return;
            pane_.Wrapper.Host.RemovePaneWrapper(pane_.Wrapper);
            paneWrapperHost.AddPaneWrapper(pane_.Wrapper);
        }

        public void ChangeCaptionBarState(CaptionBarState state_)
        { 
            CaptionBar.SetState(captionBarControl, state_);
        }
 
        public void HideDockButton()
        {
            headerControl.HideDockButton();

            foreach (ContentPaneWrapper tabItem in paneWrapperHost.Items)
            {
                tabItem.DockingGrid.HideDockButton();
            }
        }
        public void ShowDockButton()
        {
            headerControl.ShowDockButton();
            foreach (ContentPaneWrapper tabItem in paneWrapperHost.Items)
            {
                tabItem.DockingGrid.ShowDockButton();
            }
        }

        public PaneContainer TearOff()
        {
            var floatingWindow = TearOffInternal();
            return floatingWindow != null ? floatingWindow.PaneContainer : null;
        }

        internal FloatingWindow TearOffInternal()
        {
            if (IsRootContainer) return null;
            if (SinglePane != null)
            {
                return SinglePane.TearOffInternal();
            }
            var floatingLocation = WPFHelper.GetPositionWithOffset(paneWrapperHost);
            DockingGrid grid = this.FindVisualParent<DockingGrid>();
            if (grid == null) return null;
            grid.Remove(this);
            FloatingWindow fw = new FloatingWindow(this);
            if (LastFloatingSize.Width > 0 && LastFloatingSize.Height > 0)
            {
                PaneHeight = LastFloatingSize.Height;
                PaneWidth = LastFloatingSize.Width;
            }
            fw.Top = floatingLocation.Y;
            fw.Left = floatingLocation.X;
            DockService.SetLastPosition(fw, WPFHelper.GetMousePosition());
            fw.Show();
            return fw;
        }
        #endregion

        static PaneContainer()
        {
           //DefaultStyleKeyProperty.OverrideMetadata(typeof(PaneContainer), new FrameworkPropertyMetadata(typeof(PaneContainer)));
            WidthProperty.OverrideMetadata(typeof(PaneContainer), new FrameworkPropertyMetadata(OnWidthChanged, 
                CoerceHeightValue));
            HeightProperty.OverrideMetadata(typeof(PaneContainer), new FrameworkPropertyMetadata(OnHeightChanged,
               CoerceWidthValue));

        }

        private static object CoerceHeightValue(DependencyObject dependencyObject_, object baseValue_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            double newValue = (double) baseValue_;
            if (newValue < container.MinHeight) return container.MinHeight;
            if (newValue > container.MaxHeight) return container.MaxHeight;
            return newValue;
        }

        private static void OnHeightChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            if (!container.syncingSize && container.IsRootContainer)
            {
                FloatingWindow w = Window.GetWindow(container) as FloatingWindow;
                if (w != null)
                {
                    w.InternalSyncSize(false, true); 
                }
            }
        }
        private static object CoerceWidthValue(DependencyObject dependencyObject_, object baseValue_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            double newValue = (double)baseValue_;
            if (newValue < container.MinWidth) return container.MinWidth;
            if (newValue > container.MaxWidth) return container.MaxWidth;
            return newValue;
        }

        private static void OnWidthChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            if (!container.syncingSize && container.IsRootContainer)
            {
                FloatingWindow w = Window.GetWindow(container) as FloatingWindow;
                if (w != null)
                {
                    w.InternalSyncSize(true, false); 
                }
            }
        }

        public PaneContainer()
        {
            InitializeComponent();
            PaneWidth = Double.NaN;
            PaneHeight = Double.NaN;
            foreach (var command in PaneContainerCommands.GetAllCommands())
            {
               CommandBindings.Add(new CommandBinding(command, ExecuteCommand, CanExecuteCommand)); 
            } 
            PreviewMouseMove += OnPaneMove;
            PreviewMouseUp += OnCaptionBarUnClick;
            PreviewMouseLeftButtonDown += PaneContainer_PreviewMouseLeftButtonDown;
            SizeChanged += PaneContainer_SizeChanged;
        }

        void PaneContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            popupSource.Width = popupTarget.Width = paneWrapperHost.ActualWidth;
            popupSource.Height = popupTarget.Height = paneWrapperHost.ActualHeight;
        }

 
        public PaneContainer(ContentPane pane_)
            : this()
        { 
            AddPane(pane_);
            PaneHeight = pane_.PaneHeight;
            PaneWidth = pane_.PaneWidth;
        }

        public void Show()
        {
            FloatingWindow window = new FloatingWindow(this);
            window.Show();
        }

        public void ShowDialog()
        {
            FloatingWindow window = new FloatingWindow(this);
            window.ShowDialog();
        }
        //public override void OnApplyTemplate()
        //{
        //    headerControl = this.GetTemplateChild("PART_Header") as PaneContainerHeaderControl;
        //    paneWrapperHost = this.GetTemplateChild("PART_WrapperHost") as ContentPaneWrapperHost;
        //    if (singlePaneWrapper != null)
        //    { 
        //        paneWrapperHost.AddPaneWrapper(singlePaneWrapper);
        //    }
        //    captionBarControl = this.GetTemplateChild("PART_CaptionBar") as Border;
        //    captionBarControl.PreviewMouseDown += OnCaptionBarClick;
        //    base.OnApplyTemplate();
        //}

        #region Helper Methods
        internal MinimizedPaneContainer MinimizedProxy { get; set; }
        internal bool IsRootContainer => MinimizedProxy == null && this.FindVisualParent<PaneContainer>() == null;


        //todo implement
        private void CanExecuteCommand(object sender_, CanExecuteRoutedEventArgs canExecuteRoutedEventArgs_)
        {
            canExecuteRoutedEventArgs_.CanExecute = true;
            canExecuteRoutedEventArgs_.Handled = true;
        }

        private void ExecuteCommand(object sender_, ExecutedRoutedEventArgs executedRoutedEventArgs_)
        {
            ExecuteCommand(executedRoutedEventArgs_.Command as RoutedCommand);
        }

        internal void OnContainerDragMove(MouseEventArgs e_)
        {
            var copy = ContainerDragMove;
            if (copy != null)
            {
                copy(this, e_);
            }
        }
        internal void OnTitleBarClick(MouseButtonEventArgs e_)
        {
            var copy = TitleBarClick;
            if (copy != null)
            {
                copy(this, e_);
            } 
        }

        internal void OnTitleBarUnClick(MouseButtonEventArgs e_)
        {
            var copy = TitleBarUnClick;
            if (copy != null)
            {
                copy(this, e_);
            }

        }
        private Point initialPosistion;
        internal void OnTabClick(object sender_, MouseButtonEventArgs e_)
        { 
            // If window is locked you cannot drag it.
            if (!IsLocked && e_.LeftButton == MouseButtonState.Pressed)
            {
                HandleTabClick(e_, sender_ as ContentPaneWrapper);
            }
        }


        private void HandleTabClick(MouseButtonEventArgs e_, ContentPaneWrapper wrapper_)
        { 
            initialPosistion = WPFHelper.GetMousePosition();

            CaptureMouse();

            if (paneWrapperHost.Items.Count == 1)
            {
                IsMoving = true;
                OnTitleBarClick(e_);
            }
            else
            {
                currentMovingPane = wrapper_.Pane;
            }
            DockManager.ActiveContainer = this;
        }

        public ContentPane SinglePane =>
            paneWrapperHost.Items.Count == 1
                ? ((ContentPaneWrapper) paneWrapperHost.Items[0]).Pane
                : null;

        public WindowState WindowStateResolved
        {
            get
            {
                var thisContainer = this;
                while (thisContainer != null)
                {
                    if (thisContainer.WindowState == WindowState.Minimized) return WindowState.Minimized;
                    thisContainer = thisContainer.FindVisualParent<PaneContainer>();
                }
                return WindowState;

            }
        } 

        public MinimizedPaneContainer MinimizedProxyResolved
        {
            get
            {
                var thisContainer = this;
                while (thisContainer != null)
                {
                    if (thisContainer.MinimizedProxy != null) return thisContainer.MinimizedProxy;
                    thisContainer = thisContainer.FindVisualParent<PaneContainer>();
                }
                return null;
            }
        }

        private void UndockPane(ContentPane pane_)
        {
            currentMovingPane = null;
            IsMoving = false;
            var newPaneContainer = pane_.TearOff();
            if (newPaneContainer == null) return;
            newPaneContainer.IsMoving = true;
            ReleaseMouseCapture();
            newPaneContainer.CaptureMouse(); 
        }

        // called whenever window dragged
        protected virtual void OnPaneMove(object sender, MouseEventArgs e)
        { 
            // If window is locked you cannot drag it.
            if (IsLocked) return;
            Point currentPosition = WPFHelper.GetMousePosition(); 
            Vector diff = new Vector(currentPosition.X - initialPosistion.X, currentPosition.Y - initialPosistion.Y);

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                  Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (currentMovingPane != null)
                { 
                    UndockPane(currentMovingPane);
                }
                else if (IsMoving)
                { 
                    OnContainerDragMove(e);
                }
            }
        }

        protected void OnCaptionBarUnClick(object sender_, MouseButtonEventArgs e_)
        {
            if (IsLocked) return;
            if (e_.LeftButton == MouseButtonState.Released)
            {
                if (IsMoving)
                {
                    OnTitleBarUnClick(e_);
                } 
                currentMovingPane = null;

                IsMoving = false;

                if (IsMouseCaptured)
                    ReleaseMouseCapture();
            }
            //when dragging pane with left mouse down, then click the right mouse, means tries to go one level down to search the candidate docking grid
            else if (e_.RightButton == MouseButtonState.Released && e_.LeftButton == MouseButtonState.Pressed)
            {
                // still dragging, so not changing IsMoving and IsTabMoving
                OnTitleBarUnClick(e_);
            }
        }
        #endregion


        public void AddPane(ContentPane pane_)
        { 
            Host.AddPaneWrapper(new ContentPaneWrapper(pane_));
 
        }
          
        public IList<ContentPane> GetChildrenPanes(bool leafOnly_=false)
        {
            return (
                from ContentPaneWrapper wrapper 
                    in Host.Items 
                where wrapper.Pane != null && (!leafOnly_ || wrapper.Pane.Layouter == null) 
                select wrapper.Pane).ToList();
        }

        public IList<PaneContainer> GetChildrenContainers()
        {
            var containers = new List<PaneContainer>();
            foreach (ContentPaneWrapper contentPaneWrapper in Host.Items)
            {
                if (contentPaneWrapper.DockingGrid != null)
                {
                    containers.AddRange(contentPaneWrapper.DockingGrid.NormalContainers);
                    containers.AddRange(contentPaneWrapper.DockingGrid.MinimizedPaneContainers.Containers);
                }
            }
            return containers;
        }

        internal bool changeSizeSuspended;
        internal void SuspendChangeSize()
        {
            changeSizeSuspended = true;
        }
        internal void ResumeChangeSize()
        {
            changeSizeSuspended = false;
        }

        internal void PerformChangeSize()
        {
            changeSizeSuspended = false;
            Host.UpdateActivePaneSize(); 
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

        //special handling when Areo is disabled
        private void PaneContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        { 
            if (IsLocked) return;
            Point pt = e.GetPosition(captionBarControl);
            var hitInfo = VisualTreeHelper.HitTest(this, pt);
            if (hitInfo == null) return;
            var hit = hitInfo.VisualHit as TabPanel;
            if (hit != null)
            {
                TabControl tab = hit.FindVisualParent<ContentPaneWrapperHost>();
                ContentPaneWrapperHost host = tab as ContentPaneWrapperHost;
                if (host == null) return;

                //check if this is the top pane
                if (!IsRootContainer)
                {
                    host.ParentContainer.HandleTabClick(e, host.ActiveWrapper);
                }
                else
                {
                    host.ParentContainer.HandleMovePane(e);
                }

                e.Handled = true;
            }
        }

 
        private void OnCaptionBarClick(object sender, MouseButtonEventArgs e)
        {
            if (IsLocked) return;
            HandleMovePane(e);
            e.Handled = true;
        }

        private void HandleMovePane(MouseButtonEventArgs e)
        { 
            ProcessDoubleClick();
            DockManager.ActiveContainer = this;
            IsMoving = true;

            initialPosistion = WPFHelper.GetMousePosition();

            CaptureMouse();

            DockButtonState = WindowButtonState.None;
            OnTitleBarClick(e);
        }

        int lastMouseCaptionClick;

        private void ProcessDoubleClick()
        {
            if (Environment.TickCount - lastMouseCaptionClick < 400)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                else if (WindowState == WindowState.Normal)
                {
                    WindowState = WindowState.Maximized;

                } 
            }

            else if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            lastMouseCaptionClick = Environment.TickCount;
        }
         

        public bool IsDockSource
        {
            get => (bool)GetValue(IsDockSourceProperty);
            internal set => SetValue(IsDockSourcePropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsDockSourcePropertyKey
        = DependencyProperty.RegisterReadOnly("IsDockSource", typeof(bool), typeof(PaneContainer), new PropertyMetadata(false, IsDockSourceChanged));

        private static void IsDockSourceChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer)dependencyObject_;
            if (container.IsDockSource)
            {
                container.popupSource.IsOpen = true;
            }
            else
            {

                container.popupSource.IsOpen = false;
            }
        }

        public static readonly DependencyProperty IsDockSourceProperty
            = IsDockSourcePropertyKey.DependencyProperty;

 

        public bool IsDockTarget
        {
            get => (bool)GetValue(IsDockTargetProperty);
            internal set => SetValue(IsDockTargetPropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsDockTargetPropertyKey
        = DependencyProperty.RegisterReadOnly("IsDockTarget", typeof(bool), typeof(PaneContainer), new PropertyMetadata(false, IsDockTargetChanged));

        private static void IsDockTargetChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            PaneContainer container = (PaneContainer) dependencyObject_;
            if (container.IsDockTarget)
            {
                container.popupTarget.IsOpen = true; 
            }
            else
            {

                container.popupTarget.IsOpen = false; 
            }
        }


        public static readonly DependencyProperty IsDockTargetProperty
            = IsDockTargetPropertyKey.DependencyProperty;
         

        public void Dispose()
        {
             //todo: implement it

            Host.Dispose(); 
        }

        internal void RecordLastFloatingSize()
        {
            var singlePane = SinglePane;
            if (singlePane != null)
            {
                singlePane.LastFloatingSize = new Size(singlePane.ActualWidth, singlePane.ActualHeight);
            }
            LastFloatingSize = new Size(ActualWidth, ActualHeight);
        }
         
    }

}
