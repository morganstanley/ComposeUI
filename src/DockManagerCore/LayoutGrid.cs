using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DockManagerCore
{
    public class LayoutGrid:Grid
    { 
        public void Clear()
        {
            foreach (var child in Children)
            {
                LayoutGrid childGrid = child as LayoutGrid;
                if (childGrid != null)
                {
                    childGrid.Clear(); 
                }  
            }
            Children.Clear();
            ColumnDefinitions.Clear();
            RowDefinitions.Clear();
            splitter = null;
            firstGrid = null;
            secondGrid = null;
            directGrid = null;
        }
 
        private readonly DockingGrid dockingGrid;
        internal LayoutGrid(DockingGrid dockingGrid_)
        {
            dockingGrid = dockingGrid_; 
        }

        private readonly SplitPanes splitPanes;
        public LayoutGrid(SplitPanes splitPanes_)
        {
            splitPanes = splitPanes_;
        }

        private LayoutGrid firstGrid;
        public LayoutGrid FirstGrid => firstGrid;

        private LayoutGrid secondGrid;
        public LayoutGrid SecondGrid => secondGrid;
        private GridSplitter splitter;
        public GridSplitter Splitter => splitter;

        private LayoutGrid directGrid;
        public LayoutGrid DirectGrid => directGrid;

        public void ArrangeLayout()
        {
            Clear();
            if (dockingGrid != null && dockingGrid.MinimizedPaneContainers.Children.Count > 0)
            {
                LayoutGrid oldLayoutGrid = dockingGrid.MinimizedPaneContainers.Parent as LayoutGrid;
                if (oldLayoutGrid != null) oldLayoutGrid.Children.Remove(dockingGrid.MinimizedPaneContainers);
                if (dockingGrid.Root != null)
                {
                    RowDefinitions.Add(new RowDefinition());
                    RowDefinition row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    RowDefinitions.Add(row);
                    ColumnDefinitions.Add(new ColumnDefinition());

                    LayoutGrid topGrid = new LayoutGrid(dockingGrid.Root); 
                    topGrid.ArrangeLayout();
                    topGrid.SetValue(RowProperty, 0);
                    dockingGrid.MinimizedPaneContainers.SetValue(RowProperty, 1);

                    Children.Add(topGrid);
                    Children.Add(dockingGrid.MinimizedPaneContainers);
                }
                else
                {

                    RowDefinition row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    RowDefinitions.Add(row);
                    ColumnDefinitions.Add(new ColumnDefinition());
                    Children.Add(dockingGrid.MinimizedPaneContainers);

                }
                return;
            }

            var panes = dockingGrid != null ? dockingGrid.Root : splitPanes;
            if (panes != null)
            {
                var visibleFirst = panes.VisibleFirst;
                var visibleSecond = panes.VisibleSecond;
                List<PaneContainer> visibleContainers = panes.GetVisibleContainers();
                if (visibleContainers.Count == 1) //already leaf
                {
                    Children.Add(visibleContainers[0]);
                }
                else if (visibleFirst == null || visibleSecond == null)
                {
                     directGrid = new LayoutGrid(visibleFirst ?? visibleSecond);
                     directGrid.ArrangeLayout();
                     Children.Add(directGrid);
                }
                else if (panes.Orientation == Orientation.Horizontal)
                {
                    RowDefinitions.Add(new RowDefinition());
                    var firstColumn = new ColumnDefinition();
                    firstColumn.SetBinding(ColumnDefinition.WidthProperty,
                                           new Binding("FirstLength") {Source = panes, Mode = BindingMode.TwoWay});
                    ColumnDefinitions.Add(firstColumn);
                    ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
                    var secondColumn = new ColumnDefinition();
                    secondColumn.SetBinding(ColumnDefinition.WidthProperty,
                                            new Binding("SecondLength") {Source = panes, Mode = BindingMode.TwoWay});
                    ColumnDefinitions.Add(secondColumn);

                    firstGrid = new LayoutGrid(visibleFirst);
                    firstGrid.ArrangeLayout();
                    secondGrid = new LayoutGrid(visibleSecond);
                    secondGrid.ArrangeLayout();

                    firstGrid.SetValue(ColumnProperty, 0);
                    secondGrid.SetValue(ColumnProperty, 2);

                    Children.Add(firstGrid);
                    Children.Add(secondGrid);

                    splitter = new GridSplitter();
                    splitter.Width = 4;
                    splitter.HorizontalAlignment = HorizontalAlignment.Right;
                    splitter.VerticalAlignment = VerticalAlignment.Stretch;
                    splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                    splitter.SetValue(ColumnProperty, 1);
                    Children.Add(splitter);
                }
                else
                {
                    var firstRow = new RowDefinition();
                    firstRow.SetBinding(RowDefinition.HeightProperty,
                                        new Binding("FirstLength") {Source = panes, Mode = BindingMode.TwoWay});
                    RowDefinitions.Add(firstRow);
                    RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
                    var secondRow = new RowDefinition();
                    secondRow.SetBinding(RowDefinition.HeightProperty,
                                         new Binding("SecondLength") {Source = panes, Mode = BindingMode.TwoWay});
                    RowDefinitions.Add(secondRow);
                    ColumnDefinitions.Add(new ColumnDefinition());

                    firstGrid = new LayoutGrid(visibleFirst);
                    firstGrid.ArrangeLayout();
                    secondGrid = new LayoutGrid(visibleSecond);
                    secondGrid.ArrangeLayout();
                    firstGrid.SetValue(RowProperty, 0);
                    secondGrid.SetValue(RowProperty, 2);

                    Children.Add(firstGrid);
                    Children.Add(secondGrid);

                    splitter = new GridSplitter();
                    splitter.Height = 4;
                    splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                    splitter.VerticalAlignment = VerticalAlignment.Bottom;
                    splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                    splitter.SetValue(RowProperty, 1);
                    Children.Add(splitter);
                }
            } 
        }

      
    }


}
