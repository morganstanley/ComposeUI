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

namespace DockManagerCore
{
    public class WindowPlainButton : WindowButton
    {
        static WindowPlainButton()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
                typeof (WindowPlainButton),
                new FrameworkPropertyMetadata(typeof (WindowPlainButton)));
        } 
         
        public DockLocation PlacementLocation
        {
            get => (DockLocation)GetValue(PlacementLocationProperty);
            set => SetValue(PlacementLocationProperty, value);
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlacementLocationProperty =
            DependencyProperty.Register("PlacementLocation", typeof(DockLocation), typeof(WindowPlainButton), new PropertyMetadata(DockLocation.None));


    }

    public enum DockLocation
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight,
        None
    }
}
