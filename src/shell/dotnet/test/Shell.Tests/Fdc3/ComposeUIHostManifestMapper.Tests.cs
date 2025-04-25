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

using Finos.Fdc3.AppDirectory;
using FluentAssertions;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

public class ComposeUIHostManifestMapperTests
{
    private readonly ComposeUIHostManifestMapper _mapper = new();

    [Fact]
    public void MapModuleDetails_Should_Return_Full_WebManifestDetails_When_HostManifest_Exists()
    {
        var fdc3App = new Fdc3App("testAppId", "testName", AppType.Web, new WebAppDetails("https://app.example.com"))
        {
            HostManifests = new Dictionary<string, object>
            {
                {
                    "ComposeUI", new ComposeUIHostManifest
                    {
                        InitialModulePosition = InitialModulePosition.Floating,
                        Height = 400.5,
                        Width = 600.8,
                        Coordinates = new() { X = 100.2, Y = 200.3 }
                    }
                }
            }
        };

        var result = _mapper.MapModuleDetails(fdc3App) as WebManifestDetails;

        result.Should().NotBeNull();
        result.Url.Should().Be(new Uri("https://app.example.com"));
        result.InitialModulePosition.Should().Be(InitialModulePosition.Floating);
        result.Width.Should().Be(600.8);
        result.Height.Should().Be(400.5);
        result.Coordinates.Should().BeEquivalentTo(new Coordinates()
        {
            X = 100.2,
            Y = 200.3
        });
    }

    [Fact]
    public void MapModuleDetails_Should_Return_WebManifestDetails_When_HostManifest_Property_Exists()
    {
        var fdc3App = new Fdc3App("testAppId", "testName", AppType.Web, new WebAppDetails("https://app.example.com"))
        {
            HostManifests = new Dictionary<string, object>
            {
                {
                    "ComposeUI", new ComposeUIHostManifest
                    {
                        InitialModulePosition = InitialModulePosition.FloatingOnly,
                        Coordinates = new (){ X = 100.2, Y = 200.3 }
                    }
                }
            }
        };

        var result = _mapper.MapModuleDetails(fdc3App) as WebManifestDetails;

        result.Should().NotBeNull();
        result.Url.Should().Be(new Uri("https://app.example.com"));
        result.InitialModulePosition.Should().Be(InitialModulePosition.FloatingOnly);
        result.Width.Should().BeNull();
        result.Height.Should().BeNull();
        result.Coordinates.Should().BeEquivalentTo(new Coordinates()
        {
            X = 100.2,
            Y = 200.3
        });
    }

    [Fact]
    public void MapModuleDetails_Should_Return_Default_WebManifestDetails_When_HostManifest_Is_Missing()
    {
        var fdc3App = new Fdc3App("testAppId", "testName", AppType.Web, new WebAppDetails("https://app.example.com"))
        {
            HostManifests = null // No host manifests
        };

        var result = _mapper.MapModuleDetails(fdc3App) as WebManifestDetails;

        result.Should().NotBeNull();
        result.Url.Should().Be(new Uri("https://app.example.com"));
        result.InitialModulePosition.Should().BeNull();
        result.Width.Should().BeNull();
        result.Height.Should().BeNull();
        result.Coordinates.Should().BeNull();
    }

}
