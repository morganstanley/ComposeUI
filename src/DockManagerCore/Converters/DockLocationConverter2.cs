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
using System.Globalization;
using System.Windows.Data;

namespace DockManagerCore.Converters
{
    public class DockLocationConverter2:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DockLocation dockLocation = (DockLocation)value;
            switch (dockLocation)
            {
                case DockLocation.TopLeft:
                    return "Dock active pane(in red)to Top Left of this pane(in green)";
                case DockLocation.Top:
                    return "Dock active pane(in red) to Top of this pane(in green)";
                case DockLocation.TopRight:
                    return "Dock active pane(in red) to Top Right of this pane(in green)";
                case DockLocation.Left:
                    return "Dock active pane(in red) to Left of this pane(in green)";
                case DockLocation.Center:
                    return "Dock active pane(in red) as a tab page this pane(in green)";
                case DockLocation.Right:
                    return "Dock active pane(in red) to Right of this pane(in green)";
                case DockLocation.BottomLeft:
                    return "Dock active pane(in red) to Bottom Left of this pane(in green)";
                case DockLocation.Bottom:
                    return "Dock active pane(in red) to Bottom of this pane(in green)";
                case DockLocation.BottomRight:
                    return "Dock active pane(in red) to Bottom Right of this pane(in green)";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
