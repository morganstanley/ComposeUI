// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Globalization;
using System.Windows;
using Finos.Fdc3;
using FluentAssertions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi;

public class SimpleResolverUiRowVisibilityConverterTests
{
    [Fact]
    public void Convert_returns_Collapsed()
    {
        var resolverUiAppData = new ResolverUiAppData()
        {
            AppId = "dummyAppId",
            AppMetadata = new AppMetadata("dummyAppId")
        };

        var converter = new SimpleResolverUiRowVisibilityConverter();

        var result = converter.Convert(resolverUiAppData, typeof(ResolverUiAppData), null, CultureInfo.InvariantCulture);

        result.Should().NotBeNull();
        result.Should().BeOfType(typeof(Visibility));
        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_returns_Visible_if_the_object_is_not_acceptable()
    {
        var converter = new SimpleResolverUiRowVisibilityConverter();

        var result = converter.Convert(new object(), typeof(ResolverUiAppData), null, CultureInfo.InvariantCulture);

        result.Should().NotBeNull();
        result.Should().BeOfType(typeof(Visibility));
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_returns_Visible_if_instanceId_exists()
    {
        var resolverUiAppData = new ResolverUiAppData()
        {
            AppId = "dummyAppId",
            AppMetadata = new AppMetadata("dummyAppId", Guid.NewGuid().ToString())
        };

        var converter = new SimpleResolverUiRowVisibilityConverter();

        var result = converter.Convert(resolverUiAppData, typeof(ResolverUiAppData), null, CultureInfo.InvariantCulture);

        result.Should().NotBeNull();
        result.Should().BeOfType(typeof(Visibility));
        result.Should().Be(Visibility.Visible);
    }
}