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

public class InitialModulePositionExtensionsTests
{
    [Theory]
    [InlineData(InitialModulePosition.Floating, InitialPaneLocation.DockableFloating)]
    [InlineData(InitialModulePosition.FloatingOnly, InitialPaneLocation.FloatingOnly)]
    public void ConvertPaneLocation_converts_InitialModulePosition_to_InitialPaneLocation(
        InitialModulePosition initialModulePosition,
        InitialPaneLocation expectedInitialPaneLocation)
    {
        var result = initialModulePosition.ConvertPaneLocation();
        result.Should().Be(expectedInitialPaneLocation);
    }

    [Fact]
    public void ConvertPaneLocation_throws_error_on_invalid_data()
    {
        var dockPosition = (InitialModulePosition)999;

        var act = () => dockPosition.ConvertPaneLocation();
        act.Should().Throw<NotSupportedException>();
    }
}
