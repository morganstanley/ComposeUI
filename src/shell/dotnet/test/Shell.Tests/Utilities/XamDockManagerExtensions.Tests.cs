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

using FluentAssertions;
using Infragistics.Windows.DockManager;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Utilities;

public class XamDockManagerExtensionsTests
{
    [Fact]
    [STAThread]
    public void OpenLocatedWebContentPane_adds_the_created_window_to_the_panes()
    {
        var options = new WebWindowOptions
        {
            Coordinates = new Coordinates { X = 100, Y = 200 },
            Width = 300,
            Height = 400,
            InitialModulePostion = InitialModulePosition.Floating
        };

        var statThread = new Thread(() => 
        {
            var xamDockManager = new XamDockManager();
            var frameworkElement = new ContentPane();

            xamDockManager.OpenLocatedWebContentPane(frameworkElement);

            xamDockManager.Panes.Count.Should().Be(1);
        });

        statThread.SetApartmentState(ApartmentState.STA);
        statThread.Start();
        statThread.Join();
    }
}
