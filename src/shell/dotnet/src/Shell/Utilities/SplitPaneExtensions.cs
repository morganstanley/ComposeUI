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
using Infragistics.Windows.DockManager;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Utilities;

internal static class SplitPaneExtensions
{
    public static void SetSplitPaneFloatingLocation(
        this SplitPane splitPane,
        Coordinates? coordinates)
    {
        if (coordinates == null)
        {
            return;
        }

        splitPane.SetValue(XamDockManager.FloatingLocationProperty, new Point(coordinates.X, coordinates.Y));
    }

    public static void SetSplitPaneFloatingSize(
        this SplitPane splitPane,
        double? width,
        double? height)
    {
        var x = width == null
            ? WebWindowOptions.DefaultWidth
            : (double) width;

        var y = height == null
            ? WebWindowOptions.DefaultHeight
            : (double) height;

        splitPane.SetValue(XamDockManager.FloatingSizeProperty, new Size(x, y));
    }
}