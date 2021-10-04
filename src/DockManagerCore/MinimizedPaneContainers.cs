using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore
{
    public class MinimizedPaneContainers : WrapPanel
    { 
        private readonly DockingGrid dockingGrid; 

        internal MinimizedPaneContainers(DockingGrid dockingGrid_)
        { 
            dockingGrid = dockingGrid_;
            Orientation = Orientation.Horizontal;
        }

        public List<PaneContainer> Containers => (from MinimizedPaneContainer minimizedPaneContainer in Children select minimizedPaneContainer.Container).ToList();

        public void HidePane(PaneContainer paneContainer_)
        {
            var button = new MinimizedPaneContainer(this, dockingGrid, paneContainer_);
            paneContainer_.MinimizedProxy = button;
            Children.Add(button); 
        }
   
        protected override Size MeasureOverride(Size constraint_)
        {
            double maxWidth = 0;
            foreach (MinimizedPaneContainer child in Children)
            {
                child.Measure(constraint_);
                var width = child.DesiredSize.Width;
                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }
            ItemWidth = maxWidth;
            return base.MeasureOverride(constraint_);
        }

    }
}
