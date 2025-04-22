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
using MorganStanley.ComposeUI.Fdc3.AppDirectory.TestUtilities;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class Fdc3ModuleCatalogTests
{
    private readonly IModuleCatalog _catalog;
    private readonly string _hostManifestName = "ComposeUI";
    public Fdc3ModuleCatalogTests()
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            path: "/apps.json",
            contents: """
            [
              {
                "appId": "app1",
                "name": "App",
                "type": "web",
                "icons": [ {
                  "src": "https://example.com/app1/icon.png",
                  "size": "256x256",
                  "type": "image/png"
                },
                {
                  "src": "https://example.com/app1/icon_small.png",
                  "size": "64x64",
                  "type": "image/png"
                }],
                "details": { "url": "https://example.com/app1" },
                "categories": [
                    "category1",
                    "category2"
                ]
              },
              {
                "appId": "app2",
                "name": "AppWithoutIcon",
                "type": "web",                
                "details": { "url": "https://example.com/app2" }
              },
              {
                "appId": "app3",
                "name": "AppWithComposeUIHostManifestDetails",
                "type": "web",                
                "details": { 
                    "url": "https://example.com/app3"
                },
                "hostManifests": {
                    "ComposeUI": {
                        "initialModulePosition": "Floating",
                        "width": 506.2,
                        "height": 303.11,
                        "coordinates": {
                            "x": 89.5,
                            "y": 45.1
                        }
                    }
                }
              },
              {
                "appId": "app4",
                "name": "AppOther",
                "type": "other",
                "details": { "url": "https://example.com/app3" }
              }
            ]
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri("file:///apps.json") },
            fileSystem: fileSystem);

        _catalog = new Fdc3ModuleCatalog(appDirectory, new MockHostManifestMapper(_hostManifestName).Object);
    }

    [Fact]
    public async Task GetManifest_returns_web_module_manifest_by_appId()
    {
        const string appId = "app1";
        const string appName = "App";
        const string tag = "category1";
        Uri appUri = new Uri("https://example.com/app1", UriKind.Absolute);
        Uri iconUri = new Uri("https://example.com/app1/icon.png", UriKind.Absolute);
        var manifest = await _catalog.GetManifest(appId);
        manifest.Should().NotBeNull();
        manifest.Id.Should().Be(appId);
        manifest.ModuleType.Should().Be(ModuleType.Web);
        manifest.Name.Should().Be(appName);
        manifest.Tags.Should().Contain(tag);
        manifest.AdditionalProperties.Should().BeEmpty();
        manifest.TryGetDetails<WebManifestDetails>(out var details).Should().BeTrue();
        details.Should().NotBeNull();
        details.Url.Should().Be(appUri);
        details.IconUrl.Should().Be(iconUri);
    }

    [Fact]
    public async Task GetManifest_without_icon_returns_web_module_manifest_by_appId()
    {
        const string appId = "app2";
        const string appName = "AppWithoutIcon";
        Uri appUri = new Uri("https://example.com/app2", UriKind.Absolute);
        var manifest = await _catalog.GetManifest(appId);
        manifest.Should().NotBeNull();
        manifest.Id.Should().Be(appId);
        manifest.ModuleType.Should().Be(ModuleType.Web);
        manifest.Name.Should().Be(appName);
        manifest.TryGetDetails<WebManifestDetails>(out var details).Should().BeTrue();
        details.Should().NotBeNull();
        details.Url.Should().Be(appUri);
        details.IconUrl.Should().BeNull();
    }


    [Fact]
    public async Task GetManifest_returns_web_module_manifest_by_appId_with_ComposeUIHostManifest_details()
    {
        var appId = "app3";
        var appName = "AppWithComposeUIHostManifestDetails";
        var appUri = new Uri("https://example.com/app3", UriKind.Absolute);
        var initialModulePosition = InitialModulePosition.Floating;
        var width = 506.2;
        var height = 303.11;
        var coordinates = new Coordinates()
        {
            X = 89.5,
            Y = 45.1
        };

        var manifest = await _catalog.GetManifest(appId);

        manifest.Should().NotBeNull();
        manifest.Id.Should().Be(appId);
        manifest.ModuleType.Should().Be(ModuleType.Web);
        manifest.Name.Should().Be(appName);
        manifest.TryGetDetails<WebManifestDetails>(out var details).Should().BeTrue();
        details.Should().NotBeNull();
        details.Url.Should().Be(appUri);
        details.IconUrl.Should().BeNull();
        details.Width.Should().Be(width);
        details.Height.Should().Be(height);
        details.Coordinates.Should().BeEquivalentTo(coordinates);
        details.InitialModulePosition.Should().Be(initialModulePosition);
    }

    [Fact]
    public async Task GetManifest_throws_NotSupportedException_for_non_web_module()
    {
        const string appId = "app4";
        Func<Task> act = async () => await _catalog.GetManifest(appId);
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"Unsupported module type: {Enum.GetName(AppType.Other)}");
    }
}
