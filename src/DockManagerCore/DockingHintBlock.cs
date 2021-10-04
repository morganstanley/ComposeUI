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
using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore
{
    public class DockingHintBlock:Control
    {
        static DockingHintBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DockingHintBlock),
                new FrameworkPropertyMetadata(typeof(DockingHintBlock)));
        }
        public DockLocation Dock
        {
            get => (DockLocation)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register("Dock", typeof(DockLocation), typeof(DockingHintBlock), new PropertyMetadata(DockLocation.None, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {

        }
    }
}
