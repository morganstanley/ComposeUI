using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DockManagerCore.Utilities;

namespace DockManagerCore.Services
{
    class GridServices
    {
        static private readonly List<FloatingWindow> windows = new List<FloatingWindow>();

        public static void Add(FloatingWindow window_)
        {
            if (!windows.Contains(window_))
            { 
                windows.Add(window_);
            } 
        } 
        public static void Remove(FloatingWindow window_)
        { 
            windows.Remove(window_);  
        }

        public static DockingGrid GetGrid(Window currentWindow_, Point point_, int depth_)
        {
            DockingGrid grid = GetTopGrid(currentWindow_, point_); 
            while (grid != null && depth_ > 0)
            {
                depth_--;
                var nextGrid = GetNextGrid(grid, point_);
                if (nextGrid == null) return grid;
                grid = nextGrid;
            } 
            return grid;
        }

        public static DockingGrid GetTopGrid(Window currentWindow_, Point point_)
        {
            FloatingWindow topWindow = null;
            DockingGrid topGrid = null;

            foreach (FloatingWindow window in windows)
            {
                if (window == currentWindow_ || 
                    window.WindowState == WindowState.Minimized || 
                    window.PaneContainer.IsLocked) continue;
                DockingGrid grid = window.PaneContainer.ActiveGrid;
                if (grid == null) continue;
                if (!grid.IsHit(point_))
                {
                    continue;
                }
                if (topWindow == null || WPFHelper.GetZIndex(window) < WPFHelper.GetZIndex(topWindow))
                {
                    topWindow = window;
                    topGrid = grid;
                }
            }
            return topGrid;
        }

        private static DockingGrid GetNextGrid(DockingGrid grid_, Point point_)
        {
            if (grid_.Root == null)
            {
                return null;
            }
            if (grid_.Layouter == null)
            {
                return null;
            }
            return GetGridForPoint(grid_.Root, grid_.Layouter, point_);
        }
         

        private static DockingGrid GetGridForPoint(SplitPanes group_, LayoutGrid grid_, Point p_)
        {

            List<PaneContainer> visibleContainers = group_.GetVisibleContainers();
            //all minimized, return null to use the upper grid
            if (visibleContainers.Count == 0)
            {
                return null;
            }
            //only one visible container, use its DockingGrid
            if (visibleContainers.Count == 1)
            {
                if (visibleContainers[0].IsLocked) return null;
                return visibleContainers[0].ActiveGrid;
            }
            var visibleFirst = group_.VisibleFirst;
            var visibleSecond = group_.VisibleSecond;
            if (visibleFirst == null && visibleSecond == null) return null;
            if (visibleSecond == null)
            {
                return GetGridForPoint(visibleFirst, grid_.DirectGrid, p_);
            }
            if (visibleFirst == null)
            {
                return GetGridForPoint(visibleSecond, grid_.DirectGrid, p_); 
            }
            Vector a, b;
            GetSeperatingLine(grid_, out a, out b);

            if (HalfPlaneTest(a, b, p_) < 0)
            { 
                return GetGridForPoint(group_.First, grid_.FirstGrid, p_);
            }
            return GetGridForPoint(group_.Second, grid_.SecondGrid, p_);

        } 

        private static void GetSeperatingLine(LayoutGrid grid_, out Vector a_, out Vector b_)
        {
            GridSplitter gridSplitter = grid_.Splitter;
            Window parentWindow = Window.GetWindow(gridSplitter);
            GeneralTransform transform = gridSplitter.TransformToAncestor(parentWindow);
            Point transformedPoint = transform.Transform(new Point(0, 0));
            Point topLeft = gridSplitter.PointToScreen(transformedPoint);

            topLeft.X -= transformedPoint.X;
            topLeft.Y -= transformedPoint.Y;

            a_ = new Vector(topLeft.X, topLeft.Y + gridSplitter.ActualHeight);
            b_ = new Vector(topLeft.X + gridSplitter.ActualWidth, topLeft.Y); ;

        }

        private static double HalfPlaneTest(Vector a, Vector b, Point c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }


    }
}
