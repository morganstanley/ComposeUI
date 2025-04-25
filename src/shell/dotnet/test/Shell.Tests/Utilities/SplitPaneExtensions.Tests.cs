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
using System.Windows.Media.Media3D;
using FluentAssertions;
using Infragistics.Windows.DockManager;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Utilities;

public class SplitPaneExtensionsTests
{
    [Fact]
    [STAThread]
    public void SetSplitPaneFloatingLocation_set_location()
    {
        var coordinates = new Coordinates()
        {
            X = 5502.6,
            Y = 359.47
        };

        var statThread = new Thread(() =>
        {
            var splitPane = new SplitPane();

            splitPane.SetSplitPaneFloatingLocation(coordinates);

            var result = (Point)splitPane.GetValue(XamDockManager.FloatingLocationProperty);

            result.Should().NotBeNull();
            result.X.Should().Be(coordinates.X);
            result.Y.Should().Be(coordinates.Y);
        });

        statThread.SetApartmentState(ApartmentState.STA);
        statThread.Start();
        statThread.Join();
    }

    [Fact]
    [STAThread]
    public void SetSplitPaneFloatingLocation_does_not_set_property()
    {
        var statThread = new Thread(() =>
        {
            var splitPane = new SplitPane();

            splitPane.SetSplitPaneFloatingLocation(null);

            var act = () => (Point) splitPane.GetValue(XamDockManager.FloatingLocationProperty);

            //The property is not set.
            act.Should().Throw<NullReferenceException>();
        });

        statThread.SetApartmentState(ApartmentState.STA);
        statThread.Start();
        statThread.Join();
    }

    [Fact]
    [STAThread]
    public void SetSplitPaneFloatingSize_set_floating_size()
    {
        var statThread = new Thread(() =>
        {
            var width = 50.1;
            var height = 288.9;

            var splitPane = new SplitPane();

            splitPane.SetSplitPaneFloatingSize(width, height);

            var result = (Size) splitPane.GetValue(XamDockManager.FloatingSizeProperty);

            result.Should().NotBeNull();
            result.Width.Should().Be(width);
            result.Height.Should().Be(height);
        });
    }

    [Fact]
    [STAThread]
    public void SetSplitPaneFloatingSize_set_to_default_values()
    {
        var statThread = new Thread(() =>
        {
            var splitPane = new SplitPane();

            splitPane.SetSplitPaneFloatingSize(null, null);

            var result = (Size) splitPane.GetValue(XamDockManager.FloatingSizeProperty);

            result.Should().NotBeNull();
            result.Width.Should().Be(WebWindowOptions.DefaultWidth);
            result.Height.Should().Be(WebWindowOptions.DefaultHeight);
        });

        statThread.SetApartmentState(ApartmentState.STA);
        statThread.Start();
        statThread.Join();
    }
}
