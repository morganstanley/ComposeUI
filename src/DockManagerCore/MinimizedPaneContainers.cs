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
