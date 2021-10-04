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
using System.Windows;
using DockManagerCore.Utilities;

namespace DockManagerCore.Services
{
    public static class DockManager
    {
        public static EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
        public static EventHandler<FloatingWindowLoadedEventArgs> FloatingWindowLoaded;
 
        private static PaneContainer activeContainer;
        public static PaneContainer ActiveContainer
        {
            get => activeContainer;
            internal set
            {
                if (activeContainer != null)
                {
                    DependencyPropertyDescriptor.FromProperty(PaneContainer.ActivePaneProperty, typeof(PaneContainer)).RemoveValueChanged(
             activeContainer, ActivePaneChangedHandler);
                    FloatingWindow fw = FloatingWindow.GetFloatingWindow(activeContainer);
                    if (!GroupManager.Contains(fw))
                    {
                        activeContainer.ChangeCaptionBarState(CaptionBarState.Unselected);
                    }

                    if (!activeContainer.IsLocked)
                        activeContainer.DockButtonState = WindowButtonState.Normal;
                }
                activeContainer = value;
                if (activeContainer != null)
                {
                    activeContainer.DockButtonState = WindowButtonState.None;

                    FloatingWindow fw2 = FloatingWindow.GetFloatingWindow(activeContainer);
                    if (!GroupManager.Contains(fw2))
                    {
                        activeContainer.ChangeCaptionBarState(CaptionBarState.Active);
                    }
                    DependencyPropertyDescriptor.FromProperty(PaneContainer.ActivePaneProperty, typeof(PaneContainer)).AddValueChanged(
                                activeContainer, ActivePaneChangedHandler);
                    activeContainer.LastActivatedTime = DateTime.UtcNow; 
                }
                if (activeContainer != null)
                {
                    ActivePane = activeContainer.ActivePane;
                }
                else
                {
                    ActivePane = null;
                }
            }
        }

        private static void ActivePaneChangedHandler(object sender_, EventArgs eventArgs_)
        {
            PaneContainer container = (PaneContainer) sender_;
            ActivePane = container.ActivePane;
        }

        private static ContentPane activePane;
        public static ContentPane ActivePane
        {
            get => activePane;
            set
            {
                if (activePane != value)
                {
                    var oldActivePane = activePane;
                    activePane = value;
                    if (activePane != null)
                    {
                        activePane.LastActivatedTime = DateTime.UtcNow; 
                    }
                    var copy = ActivePaneChanged;
                    if (copy != null)
                    {
                        copy(null, new ActivePaneChangedEventArgs(oldActivePane, activePane));
                    }
                }
            }
        }
        private static List<FloatingWindow> allWindows = new List<FloatingWindow>();

        internal static void AddWindow(FloatingWindow window_)
        {
            DockService.AttachWindow(window_);
            allWindows.Add(window_);
            var copy = FloatingWindowLoaded;
            if (copy != null)
            {
                copy(null, new FloatingWindowLoadedEventArgs(window_.PaneContainer));
            }
        }

        internal static void RemoveWindow(FloatingWindow window_)
        {
            allWindows.Remove(window_);
            DockService.DetachWindow(window_);
            var visibleWindows = allWindows.FindAll(w_ => w_.IsVisible && w_.WindowState != WindowState.Minimized);
            if (visibleWindows.Count == 1)
            {
                if (window_.Contains(ActiveContainer))
                {
                    ActiveContainer = visibleWindows[0].PaneContainer;
                }
            } 
        }

        internal static List<FloatingWindow> GetAllFloatingWindows()
        {
            var windows = new List<FloatingWindow>(allWindows);
            windows.Sort((w1, w2) => w1.LastActivatedTime.CompareTo(w2.LastActivatedTime));
            return windows;
        }
        public static List<PaneContainer> GetAllContainers()
        {
            var containers = new List<PaneContainer>();
            foreach (var floatingWindow in allWindows)
            {
                PaneContainer root = floatingWindow.PaneContainer;
                if (root != null)
                {
                    CollectPaneContainers(root, containers);
                }
            }
            return containers;
        }

        public static List<PaneContainer> GetAllContainers(this PaneContainer rootContainer_)
        {
            var containers = new List<PaneContainer>();
            CollectPaneContainers(rootContainer_, containers); 
            return containers;
        }

        public static void ExecuteActions(PaneContainer rootContainer_, Action<PaneContainer> action_)
        { 
            foreach (var container in GetAllContainers(rootContainer_))
            {
                action_(container);
            }
        }
        public static void ExecuteActions(Action<PaneContainer> action_)
        {
            foreach (var container in GetAllContainers())
            {
                action_(container);
            }
        }
        private static void CollectPaneContainers(PaneContainer parent_, List<PaneContainer> containers_)
        {
            containers_.Add(parent_);
            foreach (var directChildrenContainer in parent_.GetChildrenContainers())
            {
                CollectPaneContainers(directChildrenContainer, containers_);
            }
        }

        public static List<ContentPane> GetAllPanes(bool leafOnly_=false)
        {
            var containers = GetAllContainers();
            var panes = new List<ContentPane>();
            foreach (var paneContainer in containers)
            {
                panes.AddRange(paneContainer.GetChildrenPanes(leafOnly_));
            }
            return panes;
        }
        public static List<ContentPane> GetAllPanes(this PaneContainer rootContainer_, bool leafOnly_=false)
        {
            var containers = GetAllContainers(rootContainer_);
            var panes = new List<ContentPane>();
            foreach (var paneContainer in containers)
            {
                panes.AddRange(paneContainer.GetChildrenPanes(leafOnly_));
            }
            return panes;
        }
    }

    public class ActivePaneChangedEventArgs:EventArgs
    {
        public ActivePaneChangedEventArgs(ContentPane oldPane_, ContentPane newPane_)
        {
            OldPane = oldPane_;
            NewPane = newPane_;
        }

        public ContentPane OldPane { get; private set; }
        public ContentPane NewPane { get; private set; }
    }

    public class FloatingWindowLoadedEventArgs:EventArgs
    {
        public FloatingWindowLoadedEventArgs(PaneContainer window_)
        {
            Window = window_;
        }

        public PaneContainer Window { get; private set; }
    }
}
