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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DockManagerCore.Desktop;

namespace DockManagerCore
{
    public class SplitPanes:ViewModelBase
    {
        public SplitPanes Parent;
         
        private SplitPanes first;
        public SplitPanes First
        {
            get => first;
            set
            {
                if (value != first)
                {
                    first = value;
                    if (value != null) value.Parent = null;
                    if (first != null) first.Parent = this;
                }
            }
        }

        public SplitPanes VisibleFirst
        {
            get
            {
                if (first != null && first.Self != null)
                    return first.Self.WindowState == WindowState.Minimized ? null : first;
                if (first == null) return first;
                var visibleFirstFirst = first.VisibleFirst;
                var visibleFirstSecond = first.VisibleSecond;
                if (visibleFirstFirst == null) return visibleFirstSecond;
                if (visibleFirstSecond == null) return visibleFirstFirst;
                return first;
            }
        }
        private SplitPanes second;
        public SplitPanes Second
        {
            get => second;
            set
            {
                if (value != second)
                {
                    second = value;
                    if (value != null) value.Parent = null;
                    if (second != null) second.Parent = this;
                }
            }
        }

        public SplitPanes VisibleSecond
        {
            get
            {
                if (second != null && second.Self != null)
                    return second.Self.WindowState == WindowState.Minimized ? null : second;
                if (second == null) return null;
                var visibleSecondFirst = second.VisibleFirst;
                var visibleSecondSecond = second.VisibleSecond;
                if (visibleSecondFirst == null) return visibleSecondSecond;
                if (visibleSecondSecond == null) return visibleSecondFirst;
                return second;
            }
        }

        public PaneContainer Self { get; set; }  

        public Orientation Orientation { get; internal set; }

        private GridLength firstLength;
        public GridLength FirstLength 
        { 
            get => firstLength;
            set
            {
                if (value != firstLength)
                {

                    firstLength = value;
                    OnPropertyChanged("FirstLength");
                }
            }
        }
        private GridLength secondLength;
        public GridLength SecondLength
        {
            get => secondLength;
            set
            {
                if (value != secondLength)
                {
                    secondLength = value; 
                    OnPropertyChanged("SecondLength");
                }
            }
        }
 
        
        public SplitPanes()
        {

        }

        public SplitPanes(PaneContainer p_)
        {
            Self = p_;
        }

        public SplitPanes(SplitPanes first_, SplitPanes second_, Orientation orientation_)
        {
            First = first_;
            Second = second_;
            Orientation = orientation_;
            FirstLength = new GridLength(1, GridUnitType.Star);
            SecondLength = new GridLength(1, GridUnitType.Star);
        }
 
        public static SplitPanes FindOwnerSplitPanes(SplitPanes root_, PaneContainer container_)
        {
            if (root_.Self == container_) return root_.Parent;
            if (root_.First != null)
            {
                var splitPanes = FindOwnerSplitPanes(root_.First, container_);
                if (splitPanes != null) return splitPanes.Parent;
            }
            if (root_.Second != null)
            {
                var splitPanes = FindOwnerSplitPanes(root_.Second, container_);
                if (splitPanes != null) return splitPanes.Parent;
            }
            return null;
        }

        private void CollectAllVisibleContainers(List<PaneContainer> containers_)
        {
            if (Self != null && Self.WindowState != WindowState.Minimized)
            {
                containers_.Add(Self);
            }
            if (First != null) First.CollectAllVisibleContainers(containers_);
            if (Second != null) Second.CollectAllVisibleContainers(containers_); 
        }

        public List<PaneContainer> GetVisibleContainers()
        {
            List<PaneContainer> visibleContainers = new List<PaneContainer>();
            CollectAllVisibleContainers(visibleContainers);
            return visibleContainers;
        }

        public void HideDockButton()
        {
            if (Self != null)
                Self.DockButtonState = WindowButtonState.None;
            else
            {
                First.HideDockButton();
                Second.HideDockButton();
            }
        }

        public void ShowDockButton()
        {
            if (Self != null)
            {
                if (!Self.IsLocked)
                {
                    Self.DockButtonState = WindowButtonState.Normal;
                }
            }
            else
            {
                First.ShowDockButton();
                Second.ShowDockButton();
            }
        }
    }
}
