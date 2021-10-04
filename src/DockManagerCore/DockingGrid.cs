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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DockManagerCore.Desktop;
using DockManagerCore.Services;

namespace DockManagerCore
{
    internal class DockingGrid : ContentControl, IDisposable
    {
        public ContentPaneWrapper Wrapper
        {
            get; private set; }
        
        public LayoutGrid Layouter => RootPane.Layouter;

        public SplitPanes Root { get; private set; }

        public MinimizedPaneContainers MinimizedPaneContainers { get; private set; }

        public List<PaneContainer> NormalContainers => Root == null ? new List<PaneContainer>() : Root.GetVisibleContainers();

        public bool IsEmpty => MinimizedPaneContainers.Children.Count == 0 && NormalContainers.Count == 0 && originalPane == null;
        private ContentPane rootPane;
        public ContentPane RootPane
        {
            get => rootPane;
            set
            {
                rootPane = value; 
                Wrapper.Pane = value;
            }
        }
         
        public DockingGrid()
        { 
            MinimizedPaneContainers = new MinimizedPaneContainers(this); 
            Margin = new Thickness(-3); 
        }

        private ContentPane originalPane;
        public DockingGrid(ContentPaneWrapper wrapper_, ContentPane pane_):this()
        {
            originalPane = pane_;
            Wrapper = wrapper_;
            Content = pane_;
            RootPane = pane_; 
        }

        public void AddTab(PaneContainer paneContainer_)
        {
            paneContainer_.RecordLastFloatingSize();
            PaneContainer parent = this.FindVisualParent<PaneContainer>();
            if (parent != null)
            {
                parent.MergePanes(paneContainer_);
            }
        }

        #region for load from layout  
        public void Load(SplitPanes splittedPanes_)
        {
            var lastActivateTime = RootPane.LastActivatedTime;
            var lastFloatingSize = RootPane.LastFloatingSize;
            var name = RootPane.Name;
            var first = Root;
            if (first == null)
            {
                PaneContainer firstPane = CreatePaneFromContent();
                first = new SplitPanes(firstPane);
            }

            Root = new SplitPanes(first, splittedPanes_, Orientation.Vertical);
            UpdatePanes(Root);
            if (Root.First != null)
            {
                Root.First.Self.Close(true); 
            }
            else if (Root.Self != null)
            {
                Root.Self.Close(true);
            }
            RootPane.LastActivatedTime = lastActivateTime;
            RootPane.LastFloatingSize = lastFloatingSize;
            RootPane.Name = name;
            ArrangeLayout();
        } 
 
        public void ArrangeLayout()
        {  
            if (Root != null)
            {
                foreach (var normalContainer in NormalContainers)
                {
                    normalContainer.ClearValue(HeightProperty);
                    normalContainer.ClearValue(WidthProperty); 
                }
            }
            Layouter.ArrangeLayout();
            if (Root == null && IsLoaded)
            {
                Wrapper.Host.UpdateActivePaneSize();
            }
        }
        public void UpdatePanes(SplitPanes panes_)
        {
            if (panes_.Self != null)
            {  
                if (ParentContainer.IsLocked)
                {
                    panes_.Self.LockButtonState = WindowButtonState.None;
                }
                if (!ParentContainer.IsHeaderVisible)
                {
                    panes_.Self.HideButtonState = WindowButtonState.None;
                }
                if (panes_.Self.WindowState == WindowState.Minimized)
                {
                    MinimizedPaneContainers.HidePane(panes_.Self);  
                }
                else
                { 
                    RegisterEvents(panes_.Self);
                } 
            }
            if (panes_.First != null)
            {
                UpdatePanes(panes_.First);
            }
            if (panes_.Second != null)
            {
                UpdatePanes(panes_.Second);
            }
        }
        #endregion

 
        public void Add(PaneContainer paneContainer_, Dock dock_, bool userAdded_=true)
        {
            if (userAdded_)
            {
                paneContainer_.RecordLastFloatingSize();  
            }
            var first = Root;
            if (first == null)
            {
                PaneContainer firstPane = CreatePaneFromContent();
                first = new SplitPanes(firstPane);
            }
            if (dock_ == Dock.Top || dock_ == Dock.Left)
            {
                Root = new SplitPanes(new SplitPanes(paneContainer_), first, dock_ == Dock.Top  ? Orientation.Vertical : Orientation.Horizontal);
            }
            else
            {
                Root = new SplitPanes(first, new SplitPanes(paneContainer_), dock_ == Dock.Bottom ? Orientation.Vertical : Orientation.Horizontal);
            }

            ArrangeLayout(); 
            RegisterEvents(paneContainer_);
            DockManager.ActiveContainer = paneContainer_; 
        }
         
        public void Add(PaneContainer paneContainer_, bool verticalFirst_, bool horizontalFirst_)
        {
            if (Root != null && Root.First != null && Root.Second != null)
            {
                paneContainer_.RecordLastFloatingSize();
                Orientation orientation = Root.Orientation == Orientation.Vertical ? Orientation.Horizontal : Orientation.Vertical;
                bool first1 = Root.Orientation == Orientation.Vertical ? verticalFirst_ : horizontalFirst_;
                bool first2 = Root.Orientation == Orientation.Vertical ? horizontalFirst_ : verticalFirst_;
                if (first1)
                {
                     Root.First = first2 ? new SplitPanes(new SplitPanes(paneContainer_), Root.First, orientation) : 
                                           new SplitPanes(Root.First, new SplitPanes(paneContainer_), orientation);
                }
                else
                {
                    Root.Second = first2 ? new SplitPanes(new SplitPanes(paneContainer_), Root.Second, orientation) : 
                                           new SplitPanes(Root.Second, new SplitPanes(paneContainer_), orientation);
                } 
                ArrangeLayout();
                RegisterEvents(paneContainer_);
                DockManager.ActiveContainer = paneContainer_;
            }
            else
            {
                Add(paneContainer_, verticalFirst_ ? Dock.Top : Dock.Bottom);
            }
        }

        public bool IsHit(Point p_)
        {
            Window parentWindow = this.FindVisualParent<Window>();
            if (parentWindow == null) return false;
            GeneralTransform transform = TransformToAncestor(parentWindow);
            Point transformedPoint = transform.Transform(new Point(0, 0));
            Point topLeft = PointToScreen(transformedPoint);
            double top = topLeft.Y - transformedPoint.Y;
            double left = topLeft.X - transformedPoint.X;
            double bottom = top + ActualHeight;
            double right = left + ActualWidth; 
            return (p_.X > left && p_.X < right && p_.Y > top && p_.Y < bottom);
        } 
         
        public void HideDockButton()
        {
            if (Root != null)
                Root.HideDockButton();
        }

        public void ShowDockButton()
        {
            if (Root != null)
                Root.ShowDockButton();
        }
 
        internal void Remove(PaneContainer p_)
        {
            UnRegisterEvents(p_);
            // remove from the tree
            Remove(Root, p_);

            bool minimized = false;

            //there is only one pane now, and is minimized (can be caused by minize one pane, and close all other panes) 
            if (Root != null && NormalContainers.Count == 0 && MinimizedPaneContainers.Children.Count == 1)
            { 
                MinimizedPaneContainers.Containers.First().ExecuteCommand(PaneContainerCommands.Restore);
                minimized = true;
            }

            // if root has an attached pane, we need to merge it with the parent pane
            // unless there are some minimized panes
            if (Root != null && Root.Self != null && MinimizedPaneContainers.Children.Count == 0)
            { 
                PaneContainer paneContainer = ParentContainer;
                paneContainer.MergePanes(Root.Self);
                Wrapper.Host.RemovePaneWrapper(Wrapper); 
                Layouter.Clear(); 
                Root = null;
                if (minimized && paneContainer.Host.Items.Count == 1)
                {
                    paneContainer.WindowState = WindowState.Minimized;
                } 
            }
            ArrangeLayout(); 
        }

        private PaneContainer CreatePaneFromContent()
        {
            originalPane = null;
            var rootPaneCopied = RootPane;
            Content = RootPane = new ContentPane(this);
            PaneContainer newPaneContainer = new PaneContainer(rootPaneCopied);
            PaneContainer parent = ParentContainer; 

            newPaneContainer.PaneWidth = parent.ActualWidth;
            newPaneContainer.PaneHeight = parent.ActualHeight;
            if (parent == DockManager.ActiveContainer)
            {
                DockManager.ActiveContainer = newPaneContainer; 
            }
            else
            {
                newPaneContainer.ChangeCaptionBarState(CaptionBar.GetState(parent.captionBarControl));
            }
            parent.Host.UpdateTabItems();
            
            RegisterEvents(newPaneContainer);
            return newPaneContainer;
        }
 
        private void Remove(SplitPanes g_, PaneContainer p_)
        {
            if (p_ == DockManager.ActiveContainer)
            {
                DockManager.ActiveContainer = ParentContainer; 
            }
            if (g_ == null || g_.Self != null)
            {
                return;
            }
            if (g_.First.Self == p_)
            {
                Replace(g_, g_.Second);
            }
            else if (g_.Second.Self == p_)
            {
                Replace(g_, g_.First);
            }
            else if (g_.First != null && g_.Second != null)
            {
                Remove(g_.First, p_);
                Remove(g_.Second, p_);
            }
        }

        private void Replace(SplitPanes parent_, SplitPanes child_)
        {
            if (child_.Self != null)
            {
                parent_.Self = child_.Self;
                parent_.First = null;
                parent_.Second = null;
            }
            else
            {
                parent_.First = child_.First;
                parent_.Second = child_.Second;
                parent_.Orientation = child_.Orientation;
                parent_.FirstLength = child_.FirstLength;
                parent_.SecondLength = child_.SecondLength;

            }
        }

        public void RegisterEvents(PaneContainer p_)
        {
            p_.ContainerStateChanged += StateChange; 
            p_.ContainerCloseRequest += HandleClose;
            p_.ContainerDragMove += ContainerDragMove; 
        }

        public void UnRegisterEvents(PaneContainer p_)
        {
            p_.ContainerStateChanged -= StateChange; 
            p_.ContainerCloseRequest -= HandleClose;
            p_.ContainerDragMove -= ContainerDragMove; 
             
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

        private void StateChange(object sender_, EventArgs e_)
        {
            PaneContainer paneContainer = sender_ as PaneContainer;

            if (paneContainer.WindowState == WindowState.Minimized && !paneContainer.IsLocked)
            {
                MinimizedPaneContainers.HidePane(paneContainer); 
                UnRegisterEvents(paneContainer);
                ArrangeLayout();
            }
            else if (paneContainer.WindowState == WindowState.Maximized && !paneContainer.IsLocked)
            {
                var floatingWindow = paneContainer.TearOffInternal();
                if (floatingWindow == null) return;
                floatingWindow.WindowState = WindowState.Maximized; 
            } 
        }
 
 
        private void HandleClose(object sender_, HandledEventArgs e_)
        {
            PaneContainer paneContainer = sender_ as PaneContainer; 
            Remove(paneContainer);
            e_.Handled = true;
        }

        private void ContainerDragMove(object sender_, MouseEventArgs e_)
        {
            PaneContainer paneContainer = (PaneContainer)sender_; 
            var newPaneContainer = paneContainer.TearOff();
            if (newPaneContainer == null) return; 
            // re-capture the mouse because it's in an other control
            newPaneContainer.CaptureMouse();
        }
         

        public void Dispose()
        {
            //todo: implement it
        }
    }
}
