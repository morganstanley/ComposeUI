using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DockManagerCore.Desktop;
using DockManagerCore.Services;

namespace DockManagerCore
{
    public class MinimizedPaneContainer:Button
    { 
        private readonly DockingGrid dockingGrid;
        private readonly MinimizedPaneContainers paneContainers; 
        static MinimizedPaneContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
    typeof(MinimizedPaneContainer),
    new FrameworkPropertyMetadata(typeof(MinimizedPaneContainer)));

        }
        internal MinimizedPaneContainer(MinimizedPaneContainers paneContainers_, DockingGrid dockingGrid_, PaneContainer paneContainer_)
        {
            paneContainers = paneContainers_;
            dockingGrid = dockingGrid_; 
            Container = paneContainer_;
            Pane = paneContainer_.ActivePane;
            DataContext = Pane;
            Command = new DelegateCommand(_ => RestorePane());
            Container.ContainerCloseRequest += HandleCloseRequest; 
        }

        public ContentPane Pane
        {
            get => (ContentPane)GetValue(PaneProperty);
            private set => SetValue(PanePropertyKey, value);
        }

        private static readonly DependencyPropertyKey PanePropertyKey
        = DependencyProperty.RegisterReadOnly("Pane", typeof(ContentPane), typeof(MinimizedPaneContainer), new PropertyMetadata(null));

        public static readonly DependencyProperty PaneProperty
            = PanePropertyKey.DependencyProperty; 

        public PaneContainer Container
        {
            get => (PaneContainer)GetValue(ContainerProperty);
            private set => SetValue(ContainerPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ContainerPropertyKey
        = DependencyProperty.RegisterReadOnly("Container", typeof(PaneContainer), typeof(MinimizedPaneContainer), new PropertyMetadata(null));

        public static readonly DependencyProperty ContainerProperty
            = ContainerPropertyKey.DependencyProperty; 

        private void Detach()
        { 
            Container.ContainerCloseRequest -= HandleCloseRequest;
            Container.MinimizedProxy = null;
            paneContainers.Children.Remove(this); 
        }

        private void RestorePane()
        {
            Detach();
            Container.WindowState = WindowState.Normal;
            dockingGrid.RegisterEvents(Container);
            dockingGrid.ArrangeLayout();
            DockManager.ActiveContainer = Container; 

        }

        public void RemoveFromParent()
        {
             Detach();
            dockingGrid.Remove(Container);
            dockingGrid.ArrangeLayout();
            if (dockingGrid != null && dockingGrid.IsEmpty)
            {
                dockingGrid.Wrapper.Host.Close(dockingGrid.Wrapper, true);
            }
        }
         
        private void HandleCloseRequest(object sender_, HandledEventArgs eventArgs_)
        {
            RemoveFromParent();
            eventArgs_.Handled = true;
        }
    }
}
