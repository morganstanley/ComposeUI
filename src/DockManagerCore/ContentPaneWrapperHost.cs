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
using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore
{
    public class ContentPaneWrapperHost:TabControl, IDisposable
    { 
     
        public ContentPaneWrapperHost()
        {
            Loaded += ContentPaneWrapperHost_Loaded;
        }

        void ContentPaneWrapperHost_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += ContentPaneWrapperHost_SizeChanged;
        }

        void ContentPaneWrapperHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ParentContainer.changeSizeSuspended) return;
            UpdateActivePaneSize();
        }

        internal void UpdateActivePaneSize()
        {
            if (ActiveWrapper != null && ActiveWrapper.Pane != null)
            {
                ActiveWrapper.Pane.PaneWidth = ParentContainer.ActualWidth;
                ActiveWrapper.Pane.PaneHeight = ParentContainer.ActualHeight;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ContentPaneWrapper wrapper = e.AddedItems[0] as ContentPaneWrapper;
                if (wrapper != null)
                {
                    ActiveWrapper = wrapper;
                }
            }
            base.OnSelectionChanged(e);
        }

        public PaneContainer ParentContainer
        {
            get => (PaneContainer)GetValue(ParentContainerProperty);
            set => SetValue(ParentContainerProperty, value);
        }

        // Using a DependencyProperty as the backing store for ParentContainer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentContainerProperty =
            DependencyProperty.Register("ParentContainer", typeof(PaneContainer), typeof(ContentPaneWrapperHost), new PropertyMetadata(null));


        public ContentPaneWrapper ActiveWrapper
        {
            get => (ContentPaneWrapper)GetValue(ActiveWrapperProperty);
            internal set => SetValue(ActiveWrapperPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ActiveWrapperPropertyKey
        = DependencyProperty.RegisterReadOnly("ActiveWrapper", typeof(ContentPaneWrapper), typeof(ContentPaneWrapperHost), new PropertyMetadata(null, ActiveWrapperChanged));

        public static readonly DependencyProperty ActiveWrapperProperty
            = ActiveWrapperPropertyKey.DependencyProperty;

     
        private static void ActiveWrapperChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            ContentPaneWrapperHost host = (ContentPaneWrapperHost) dependencyObject_;
            if (host.ParentContainer != null)
            {
                if (host.ActiveWrapper != null)
                {
                    if (host.IsLoaded && !host.ParentContainer.changeSizeSuspended)
                    { 
                        host.UpdateActivePaneSize();
                    }
                    host.ActiveWrapper.IsSelected = true;
                }
                if (dependencyPropertyChangedEventArgs_.NewValue == null)
                {
                    host.ParentContainer.ActivePane = null;
                }
                else
                {
                    host.ParentContainer.ActivePane = ((ContentPaneWrapper)dependencyPropertyChangedEventArgs_.NewValue).Pane;
                }
            }
        }


        public void AddPaneWrapper(ContentPaneWrapper paneContainer_)
        {
            paneContainer_.Host = this;
            paneContainer_.CloseTab += OnTabClose;
            paneContainer_.ClickTab += ParentContainer.OnTabClick;
            Items.Add(paneContainer_);

            UpdateTabItems();
        }


        public void RemovePaneWrapper(ContentPaneWrapper paneContainer_)
        { 
            paneContainer_.CloseTab -= OnTabClose;
            paneContainer_.ClickTab -= ParentContainer.OnTabClick;

            Items.Remove(paneContainer_);
            if (ActiveWrapper == paneContainer_)
            {
                ActiveWrapper = null;
            }
 
            UpdateTabItems();
             
        }

        internal void UpdateTabItems()
        {
            bool singleTab = Items.Count < 2;

            foreach (ContentPaneWrapper item in Items)
            {
                item.IsSingleTab = singleTab; 
                if (singleTab || item.IsSelected)
                {
                    ActiveWrapper = item; 
                } 
            }
        }

        
        internal bool Close(ContentPaneWrapper wrapper_, bool emptyWrapper_)
        {
            if (wrapper_ == null || !Items.Contains(wrapper_) 
                //|| ParentContainer.IsLocked
                ) return true;
            ClosingEventArgs arg = new ClosingEventArgs(emptyWrapper_);
            wrapper_.Pane.OnClosing(arg);
            if (arg.Cancel) return false;
            RemovePaneWrapper(wrapper_);
            wrapper_.Pane.OnClosed();
            wrapper_.Dispose();

            //closed the last pane
            if (Items.Count == 0)
            {
                ParentContainer.Close(emptyWrapper_);
            }
            return true;
        }

        private void OnTabClose(object source_, RoutedEventArgs args_)
        {
            Close(args_.OriginalSource as ContentPaneWrapper, false); 
        }


        public void Dispose()
        {
             //todo: implement it
            foreach (ContentPaneWrapper contentPaneWrapper in Items)
            {
                contentPaneWrapper.Dispose();
            }
        }
    }
}
