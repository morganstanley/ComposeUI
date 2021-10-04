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
using DockManagerCore.Services;

namespace DockManagerCore
{
    /// <summary>
    /// Interaction logic for PaneContainerHeaderControl.xaml
    /// </summary>
    public partial class PaneContainerHeaderControl : Border
    {
         public PaneContainerHeaderControl()
        {
            InitializeComponent();
            _restoreButton.Visibility = Visibility.Collapsed;
            Loaded += PaneHeaderControl_Loaded;
        }

        void PaneHeaderControl_Loaded(object sender_, RoutedEventArgs e_)
        {
            if (PaneContainer == null) return;
            PaneContainerIsLockedChanged(PaneContainer, e_);
            PaneContainerContainerHeaderVisibleChanged(PaneContainer, e_);
            PaneContainer.ContainerIsLockedChanged += PaneContainerIsLockedChanged;
            PaneContainer.ContainerStateChanged += PaneContainerContainerStateChanged;
            PaneContainer.ContainerHeaderVisibleChanged += PaneContainerContainerHeaderVisibleChanged;
            _dockPopUp.Closed += _dockPopUp_Closed;
        }

        void _dockPopUp_Closed(object sender_, EventArgs e_)
        {
            if (DockManager.ActiveContainer != null)
            {
                DockManager.ActiveContainer.IsDockTarget = DockManager.ActiveContainer.IsDockSource = false;
            }
            PaneContainer.IsDockSource = PaneContainer.IsDockTarget = false;
        }

        public PaneContainer PaneContainer
        {
            get => (PaneContainer)GetValue(PaneContainerProperty);
            set => SetValue(PaneContainerProperty, value);
        }

        // Using a DependencyProperty as the backing store for Pane.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PaneContainerProperty =
            DependencyProperty.Register("PaneContainer", typeof(PaneContainer), typeof(PaneContainerHeaderControl), new PropertyMetadata(null));  
 
      
        public void HideDockButton()
        {
            _dockButton.Visibility = Visibility.Collapsed;
 
        }
        public void ShowDockButton()
        {
            if (!PaneContainer.IsLocked)
                _dockButton.Visibility = Visibility.Visible; 
        }
        public void HideLockButtons()
        {
            _unlockButton.Visibility = _lockButton.Visibility = Visibility.Collapsed;

        }
        void PaneContainerContainerHeaderVisibleChanged(object sender_, EventArgs e_)
        {
            if (PaneContainer.HideButtonState == WindowButtonState.None) return;
            if (PaneContainer.IsHeaderVisible)
            {
                if (PaneContainer.IsLocked)
                {
                    _headerButton.Visibility = Visibility.Collapsed;
                    _noHeaderButton.Visibility = Visibility.Visible;
                    if (PaneContainer.LockButtonState != WindowButtonState.None)
                    {
                        _unlockButton.Visibility = Visibility.Visible;
                    } 
                }
                else
                {
                    _headerButton.Visibility = Visibility.Collapsed;
                    _noHeaderButton.Visibility = Visibility.Visible; 
                }
            }
            else
            {  
                _noHeaderButton.Visibility = Visibility.Collapsed;
                _headerButton.Visibility = Visibility.Visible;
                HideLockButtons();
            }
        }


        void PaneContainerIsLockedChanged(object sender_, EventArgs e_)
        {
            if (PaneContainer.IsLocked)
            {
                if (PaneContainer.LockButtonState != WindowButtonState.None)
                {
                    _lockButton.Visibility = Visibility.Collapsed;
                    _unlockButton.Visibility = Visibility.Visible;
                } 
                if (PaneContainer.HideButtonState != WindowButtonState.None)
                {
                    _noHeaderButton.Visibility = Visibility.Visible;
                    _headerButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (PaneContainer.LockButtonState != WindowButtonState.None)
                {
                    _unlockButton.Visibility = Visibility.Collapsed;
                    _lockButton.Visibility = Visibility.Visible;
                }
                if (PaneContainer.HideButtonState != WindowButtonState.None)
                {
                    _noHeaderButton.Visibility = Visibility.Collapsed;
                    _headerButton.Visibility = Visibility.Collapsed;
                }

            }
            RefreshWindowState();
        }


        private void RefreshWindowState()
        { 
            // Normal
            if (PaneContainer.WindowState == WindowState.Normal)
            {
                _restoreButton.Visibility = Visibility.Collapsed; 

                // if Maximize button state is 'None' (button is explicitly hidden by developer) => 
                // do not make visible
                if (PaneContainer.MaximizeButtonState != WindowButtonState.None)
                    _maximizeButton.Visibility = Visibility.Visible;
            }
            // Maximized
            else if (PaneContainer.WindowState == WindowState.Maximized)
            {
                _maximizeButton.Visibility = Visibility.Collapsed; 

                // if Maximize button state is 'None' (button is explicitly hidden by developer) => 
                // do not make visible
                if (PaneContainer.MaximizeButtonState != WindowButtonState.None)
                    _restoreButton.Visibility = Visibility.Visible;
            }
            else if (PaneContainer.WindowState == WindowState.Minimized)
            {
                _restoreButton.Visibility = Visibility.Visible;
                if (PaneContainer.MaximizeButtonState != WindowButtonState.None)
                {
                    _maximizeButton.Visibility = Visibility.Visible; 
                }
            }
        }
        public void PaneContainerContainerStateChanged(object sender_, EventArgs e_)
        {
            RefreshWindowState();
        } 

        protected void OnButtonDock_Click(object sender_, RoutedEventArgs e_)
        {
            if (DockManager.ActiveContainer == null || DockManager.ActiveContainer == PaneContainer)
            {
                PaneContainer.DockButtonState = WindowButtonState.None;
                return;
            }
            _dockPopUp.IsOpen = true;
            DockManager.ActiveContainer.IsDockSource = true;
            PaneContainer.IsDockTarget = true; 
        }

     
        private void OnPopupClick(object sender_, RoutedEventArgs e_)
        {
            WindowPlainButton button = (WindowPlainButton)sender_;
            if (DockManager.ActiveContainer == null || DockManager.ActiveContainer == PaneContainer)
            {
                _dockPopUp.IsOpen = false;
                PaneContainer.DockButtonState = WindowButtonState.None;
            }
              _dockPopUp.IsOpen = false;
              DockService.DockContainer(DockManager.ActiveContainer, PaneContainer, button.PlacementLocation); 
        }

        private void Button_OnClick(object sender_, RoutedEventArgs e_)
        {
            DockManager.ActiveContainer = PaneContainer;
        }

    }
}
