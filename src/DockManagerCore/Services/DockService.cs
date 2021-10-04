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
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DockManagerCore.Desktop;
using DockManagerCore.Utilities;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace DockManagerCore.Services
{
    class DockService
    { 
        private static readonly DockingPlaceholder ScreenDockingPlaceholder = new DockingPlaceholder {IsGlobalDocker = true};
        public static bool BorderDocking = true;
        public static double DockingThreshold = 150;
        public static double DockingFactor = 0.2;
        public static double DockingPixels = 200;
        #region Attached Properties
        public static readonly DependencyProperty LastPositionProperty =
            DependencyProperty.RegisterAttached("LastPosition", typeof (Point), typeof (DockService), new PropertyMetadata(default(Point)));

        public static Key GroupKey = Key.LeftCtrl;
        public static Key DockToScreenKey = Key.Space;
        public static Key StickToScreenKey = Key.LeftAlt; 
        public static Key StickToWindowKey = Key.LeftShift;

        public static void SetLastPosition(FloatingWindow element_, Point value_)
        {
            element_.SetValue(LastPositionProperty, value_);
        }

        public static Point GetLastPosition(FloatingWindow element_)
        {
            return (Point) element_.GetValue(LastPositionProperty);
        }

        public static readonly DependencyProperty DockPositionProperty =
            DependencyProperty.RegisterAttached("DockPosition", typeof(DockPosition), typeof(DockService), new PropertyMetadata(DockPosition.None));

        public static void SetDockPosition(FloatingWindow element_, DockPosition value_)
        {
            element_.SetValue(DockPositionProperty, value_);
        }

        public static DockPosition GetDockPosition(FloatingWindow element_)
        {
            return (DockPosition)element_.GetValue(DockPositionProperty);
        }


        public static readonly DependencyProperty DockPlaceHolderProperty =
            DependencyProperty.RegisterAttached("DockPlaceHolder", typeof (DockingPlaceholder), typeof (DockService), new PropertyMetadata(null));

        public static void SetDockPlaceHolder(FloatingWindow element_, DockingPlaceholder value_)
        {
            element_.SetValue(DockPlaceHolderProperty, value_);
        }

        public static DockingPlaceholder GetDockPlaceHolder(FloatingWindow element_)
        {
            return (DockingPlaceholder) element_.GetValue(DockPlaceHolderProperty);
        }
 
        public static readonly DependencyProperty CurrentDepthProperty =
            DependencyProperty.RegisterAttached("CurrentDepth", typeof (int), typeof (DockService), new PropertyMetadata(default(int)));

        public static void SetCurrentDepth(FloatingWindow element_, int value_)
        {
            element_.SetValue(CurrentDepthProperty, value_);
        }

        public static int GetCurrentDepth(FloatingWindow element_)
        {
            return (int)element_.GetValue(CurrentDepthProperty);
        }


        public static readonly DependencyProperty CurrentGridProperty =
            DependencyProperty.RegisterAttached("CurrentGrid", typeof (DockingGrid), typeof (DockService), new PropertyMetadata(default(DockingGrid)));

        public static void SetCurrentGrid(FloatingWindow element_, DockingGrid value_)
        {
            element_.SetValue(CurrentGridProperty, value_);
        }

        public static DockingGrid GetCurrentGrid(FloatingWindow element_)
        {
            return (DockingGrid) element_.GetValue(CurrentGridProperty);
        }
 
         
        #endregion

        public static void AttachWindow(FloatingWindow window_)
        {
              
            window_.Deactivated += WindowDeactivated;
            window_.Activated += WindowActivated;
            window_.PaneContainer.ContainerDragMove += WindowDragMove;
            window_.PaneContainer.TitleBarClick += WindowTitleBarClick;
            window_.PaneContainer.TitleBarUnClick += WindowTitleBarUnClick;
            window_.KeyDown += CaptureKey;

            var placeHolder = new DockingPlaceholder
                {
                    Visibility = Visibility.Hidden
                };
            SetDockPlaceHolder(window_, placeHolder);  
            ScreenDockingPlaceholder.Hide();   
        }

        static void WindowDeactivated(object sender, EventArgs e)
        {
            FloatingWindow window = (FloatingWindow)sender;

            if (!window.IsVisible && window.Contains(DockManager.ActiveContainer))
            {
                DockManager.ActiveContainer = null;
            }
        }

        static void WindowActivated(object sender, EventArgs e)
        {
            FloatingWindow window = (FloatingWindow) sender;
            window.LastActivatedTime = DateTime.UtcNow; 
            if (DockManager.ActiveContainer == null || !DockManager.ActiveContainer.IsVisible)
            {
                DockManager.ActiveContainer = window.PaneContainer;
            }
        }

        public static void DetachWindow(FloatingWindow window_)
        {
            window_.Deactivated -= WindowDeactivated;
            window_.Activated -= WindowActivated;
            window_.PaneContainer.ContainerDragMove -= WindowDragMove;
            window_.PaneContainer.TitleBarClick -= WindowTitleBarClick;
            window_.PaneContainer.TitleBarUnClick -= WindowTitleBarUnClick;
            window_.KeyDown -= CaptureKey; 
            window_.ClearValue(LastPositionProperty);
            window_.ClearValue(DockPlaceHolderProperty);
            window_.ClearValue(DockPositionProperty);
            window_.ClearValue(CurrentDepthProperty);
            window_.ClearValue(CurrentGridProperty); 
        }

        public static void DockContainer(PaneContainer sourceContainer_, PaneContainer targetContainer_, DockLocation dockLocation_)
        {
            var parentPane = targetContainer_.FindVisualParent<PaneContainer>();
            var targetContainer = targetContainer_; 

            if (sourceContainer_.IsRootContainer)
            {
                FloatingWindow floatingWindow = FloatingWindow.GetFloatingWindow(sourceContainer_);
                if (floatingWindow != null)
                {
                    floatingWindow.CloseInternal();
                }
            }
            else if (sourceContainer_.MinimizedProxy != null)
            {
                sourceContainer_.MinimizedProxy.RemoveFromParent();
            }
            else
            {
                DockingGrid parentDockingGrid = sourceContainer_.FindVisualParent<DockingGrid>();
                if (parentDockingGrid != null)
                {
                    parentDockingGrid.Remove(sourceContainer_);
                }
            }
            DockingGrid dockingGrid = null;
            if (targetContainer_.paneWrapperHost.Items.Count == 0 && parentPane != null)//no pane now
            {
                targetContainer = parentPane;  
            }
            dockingGrid = targetContainer.ActiveGrid;

            if (dockingGrid == null) return;

            sourceContainer_.IsDockSource = sourceContainer_.IsDockTarget = false;
            targetContainer_.IsDockSource = targetContainer_.IsDockTarget = false;
            if (dockLocation_ == DockLocation.TopLeft)
            {
                dockingGrid.Add(sourceContainer_, true, true);
            }
            else if (dockLocation_ == DockLocation.Top)
            {
                dockingGrid.Add(sourceContainer_, Dock.Top);
            }
            else if (dockLocation_ == DockLocation.TopRight)
            {
                dockingGrid.Add(sourceContainer_, true, false);
            }
            else if (dockLocation_ == DockLocation.Left)
            {
                dockingGrid.Add(sourceContainer_, Dock.Left);
            }
            else if (dockLocation_ == DockLocation.Center)
            {
                targetContainer.MergePanes(sourceContainer_);
            }
            else if (dockLocation_ == DockLocation.Right)
            {
                dockingGrid.Add(sourceContainer_, Dock.Right);
            }
            else if (dockLocation_ == DockLocation.BottomLeft)
            {
                dockingGrid.Add(sourceContainer_, false, true);
            }
            else if (dockLocation_ == DockLocation.Bottom)
            {
                dockingGrid.Add(sourceContainer_, Dock.Bottom);
            }
            else if (dockLocation_ == DockLocation.BottomRight)
            {
                dockingGrid.Add(sourceContainer_, false, false);
            }
            DockManager.ActiveContainer = targetContainer;
            DockManager.ActiveContainer.DockButtonState = WindowButtonState.None;
            targetContainer.IsDockSource = targetContainer.IsDockTarget = false;
        }


        static void WindowDragMove(object sender_, MouseEventArgs e_)
        {
            if (sender_ == null) return;
            FloatingWindow window = FloatingWindow.GetFloatingWindow((FrameworkElement)sender_);
            Point currentPosition = WPFHelper.GetMousePosition();
            var lastPosition = GetLastPosition(window);
            if (lastPosition == new Point(0, 0))
                lastPosition = currentPosition;

            if (lastPosition == currentPosition)
                return;

            var diff = currentPosition - lastPosition;
            bool groupMode = GroupManager.Contains(window);
            if (groupMode)
            {
                GroupManager.MoveWindows(diff);
            }
            else
            {
                window.Top += diff.Y;
                window.Left += diff.X;
            }

            //record the current position
            SetLastPosition(window, currentPosition);

            if (groupMode) return;
             
            var currentGrid = GridServices.GetGrid(window, currentPosition, GetCurrentDepth(window)); 
            //record the candidate target grid when moving window
            var placeHolder = GetDockPlaceHolder(window);
            if (currentGrid == null || currentGrid == window.PaneContainer.ActiveGrid)
            {
                placeHolder.Hide();
                SetCurrentGrid(window, null);
            }
            else
            {
                SetCurrentGrid(window, currentGrid);
                placeHolder.SetPosition(currentGrid);
            }           
            if (Keyboard.IsKeyDown(DockToScreenKey))
            {
                DisplayPlaceholder(window.Left, window.Top, window);
            }
            else
            {
                ScreenDockingPlaceholder.Hide();
                DockingUtils.HideDockBorders();
                if (BorderDocking && Keyboard.IsKeyDown(StickToScreenKey))
                {
                    DockingUtils.BorderDocking(window.Left, window.Top, window, false);
                    return;
                }
                if (Keyboard.IsKeyDown(StickToWindowKey))
                {
                    DockingUtils.SearchToDock(window.Left, window.Top, window, false);
                }
            }
        }

        static void WindowTitleBarClick(object sender_, MouseButtonEventArgs e_)
        {
            if (sender_ == null) return;
            if (e_.LeftButton != MouseButtonState.Pressed) return;
            FloatingWindow window = FloatingWindow.GetFloatingWindow((FrameworkElement)sender_);
            SetLastPosition(window, WPFHelper.GetMousePosition()); 
            //exclude current window from the target docking windows
            //GridServices.Remove(window); 
            if (Keyboard.IsKeyDown(GroupKey))
            {
                if (GroupManager.Contains(window))
                {
                    GroupManager.Remove(window); 

                    if (DockManager.ActiveContainer == window.PaneContainer)
                    {
                        window.PaneContainer.ChangeCaptionBarState(CaptionBarState.UnGrouped);
                    }
                    else
                    {
                        window.PaneContainer.ChangeCaptionBarState(CaptionBarState.Unselected);
                    }
                }
                else
                {
                    GroupManager.Add(window); 
                    window.PaneContainer.ChangeCaptionBarState(CaptionBarState.Grouped);
                }
            }
        }

        static void WindowTitleBarUnClick(object sender_, MouseEventArgs e_)
        {
            if (sender_ == null) return;
            FloatingWindow window = FloatingWindow.GetFloatingWindow((FrameworkElement)sender_);

            var currentGrid = GetCurrentGrid(window);
            var lastPosition = GetLastPosition(window);
            var currentDepth = GetCurrentDepth(window);
            var placeHolder = GetDockPlaceHolder(window);
            //only left button is down, release it, means dock the current window to current candidate grid 
            if (e_.LeftButton == MouseButtonState.Released) 
            {
                placeHolder.Hide();
                SetCurrentDepth(window, 0); //clear the depth
                if (currentGrid != null)
                {
                    DockToGrid(window, currentGrid);            
                }
                //else
                //{
                //    GridServices.Add(window); //add back current window as candidate docking target
                //}
            }
            //when dragging pane with left mouse down, then click the right mouse, means tries to go one level down to search the candidate docking grid
            else if (e_.RightButton == MouseButtonState.Released && e_.LeftButton == MouseButtonState.Pressed)
            { 
                currentGrid = GridServices.GetGrid(window, lastPosition, ++currentDepth);
                SetCurrentDepth(window, currentDepth);
                if (currentGrid == null)
                {
                    SetCurrentDepth(window, 0);
                    placeHolder.Hide();
                }
                else
                {
                    placeHolder.SetPosition(currentGrid);
                }    
            }

            bool groupMode = GroupManager.Contains(window); 
            if (Keyboard.IsKeyDown(DockToScreenKey) && !groupMode)
            {
                DockWindow(window);
            }
            else
            {
                ScreenDockingPlaceholder.Hide();
                if (!groupMode)
                {
                    if (BorderDocking && Keyboard.IsKeyDown(StickToScreenKey))
                    {
                        DockingUtils.BorderDocking(window.Left, window.Top, window, true);
                    }
                    if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        DockingUtils.SearchToDock(window.Left, window.Top, window, true);
                    }
                }
            }
            DockingUtils.HideDockBorders();
            if (DockManager.ActiveContainer != null)
            {
                DockManager.ActiveContainer.DockButtonState = WindowButtonState.None;
            } 
        }
         

        private static void CaptureKey(object sender, KeyEventArgs e)
        {
            FloatingWindow window = (FloatingWindow) sender;
            Screen s = DockingUtils.FindScreenFromWindow(window);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && ((Keyboard.IsKeyDown(Key.Left) && e.Key == Key.Down) || (Keyboard.IsKeyDown(Key.Down) && e.Key == Key.Left)))
            {
                DockingUtils.DockBottomLeft(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && ((Keyboard.IsKeyDown(Key.Down) && e.Key == Key.Right) || (Keyboard.IsKeyDown(Key.Right) && e.Key == Key.Down)))
            {
                DockingUtils.DockBottomRight(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && ((Keyboard.IsKeyDown(Key.Up) && e.Key == Key.Left) || (Keyboard.IsKeyDown(Key.Left) && e.Key == Key.Up)))
            {
                DockingUtils.DockTopLeft(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && ((Keyboard.IsKeyDown(Key.Up) && e.Key == Key.Right) || (Keyboard.IsKeyDown(Key.Right) && e.Key == Key.Up)))
            {
                DockingUtils.DockTopRight(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Left)
            {
                DockingUtils.DockLeft(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Right)
            {
                DockingUtils.DockRight(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Up)
            {
                DockingUtils.DockUp(window, s);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Down)
            {
                DockingUtils.DockDown(window, s);
            } 
        }
         

        #region Helper Methods

        private static void DisplayPlaceholder(double left_, double top_, FloatingWindow w_)
        {
            Screen s = DockingUtils.FindScreenFromWindow(w_);
            Rectangle rect = s.WorkingArea;
            double width = rect.Width;
            double height = rect.Height;
            double halfWidth = width / 2;
            double halfHeight = height / 2;
            double dockFactor = DockingFactor;
            double pixFactor = DockingPixels;
            left_ -= rect.Left;
            if (left_ < (halfWidth * dockFactor) && top_ < (halfHeight * dockFactor))
            {
                DockingUtils.DockTopLeft(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.TopLeft);
            }
            else if (left_ < (halfWidth * dockFactor) && top_ + w_.Height > (height * (1 - dockFactor)))
            {
                DockingUtils.DockBottomLeft(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.BottomLeft);
            }
            else if (left_ + w_.Width > (width * (1 - dockFactor)) && top_ < (halfHeight * dockFactor))
            {
                DockingUtils.DockTopRight(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.TopRight);
            }
            else if (left_ + w_.Width > (width * (1 - dockFactor)) && top_ + w_.Height > (height * (1 - dockFactor)))
            {
                DockingUtils.DockBottomRight(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.BottomRight);
            }
            else if (left_ < pixFactor)
            {
                DockingUtils.DockLeft(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.Left);
            }
            else if ((left_ + w_.Width) > (width - pixFactor))
            {
                DockingUtils.DockRight(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.Right);
            }
            else if (top_ < pixFactor)
            {
                DockingUtils.DockUp(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.Top);
            }
            else if (top_ + w_.Height > (height - pixFactor))
            {
                DockingUtils.DockDown(ScreenDockingPlaceholder, s);
                ScreenDockingPlaceholder.Show();
                SetDockPosition(w_, DockPosition.Bottom);
            }
            else
            {
                ScreenDockingPlaceholder.Hide();
                SetDockPosition(w_, DockPosition.None);
            }
        } 

        private static void DockToGrid(FloatingWindow window_, DockingGrid grid_)
        {
            var paneContainer = window_.PaneContainer;
            window_.CloseInternal();
            var bottomRight = new Point(grid_.ActualWidth, grid_.ActualHeight);
            Point relativePos =  WPFHelper.GetCurrentPosition(grid_); 

            if (relativePos.X < bottomRight.X / 3.0)
            {
                if (relativePos.Y < bottomRight.Y / 3.0)
                    grid_.Add(paneContainer, true, true);
                else if (relativePos.Y < bottomRight.Y * 2.0 / 3.0)
                    grid_.Add(paneContainer, Dock.Left);
                else
                    grid_.Add(paneContainer, false, true);
            }
            else if (relativePos.X < bottomRight.X * 2.0 / 3.0)
            {
                if (relativePos.Y < bottomRight.Y / 3.0)
                    grid_.Add(paneContainer, Dock.Top);
                else if (relativePos.Y < bottomRight.Y * 2.0 / 3.0)
                    grid_.AddTab(paneContainer);
                else
                    grid_.Add(paneContainer, Dock.Bottom);
            }
            else
            {
                if (relativePos.Y < bottomRight.Y / 3.0)
                    grid_.Add(paneContainer, true, false);
                else if (relativePos.Y < bottomRight.Y * 2.0 / 3.0)
                    grid_.Add(paneContainer, Dock.Right);
                else
                    grid_.Add(paneContainer, false, false);
            }

        }

        private static void DockWindow(FloatingWindow window_)
        {
            ScreenDockingPlaceholder.Hide();
            Screen s = DockingUtils.FindScreenFromWindow(window_);
            var dockPos = GetDockPosition(window_);
            switch (dockPos)
            {
                case DockPosition.None:
                    break;
                case DockPosition.BottomRight:
                    DockingUtils.DockBottomRight(window_, s);
                    break;
                case DockPosition.BottomLeft:
                    DockingUtils.DockBottomLeft(window_, s);
                    break;
                case DockPosition.Bottom:
                    DockingUtils.DockDown(window_, s);
                    break;
                case DockPosition.TopRight:
                    DockingUtils.DockTopRight(window_, s);
                    break;
                case DockPosition.TopLeft:
                    DockingUtils.DockTopLeft(window_, s);
                    break;
                case DockPosition.Top:
                    DockingUtils.DockUp(window_, s);
                    break;
                case DockPosition.Left:
                    DockingUtils.DockLeft(window_, s);
                    break;
                case DockPosition.Right:
                    DockingUtils.DockRight(window_, s);
                    break;
            }
        }

        #endregion

    }
}
