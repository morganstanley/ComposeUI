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
    [ValueConversion(typeof(DockLocation), typeof(string))]
    public class DockLocationConverter:IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DockLocation dockLocation = (DockLocation) value;
            switch (dockLocation)
            {
                case DockLocation.TopLeft:
                    return "Top Left";
                case DockLocation.Top:
                    return "Top";
                case DockLocation.TopRight:
                    return "Top Right";
                case DockLocation.Left:
                    return "Left";
                case DockLocation.Center: 
                    return "Tabbed";
                case DockLocation.Right:
                    return "Right";
                case DockLocation.BottomLeft:
                    return "Bottom Left";
                case DockLocation.Bottom:
                    return "Bottom";
                case DockLocation.BottomRight:
                    return "Bottom Right";
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
