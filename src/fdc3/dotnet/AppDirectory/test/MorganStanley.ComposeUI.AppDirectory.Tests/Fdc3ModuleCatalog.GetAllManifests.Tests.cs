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

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class Fdc3ModuleCatalogTests
{
    // unit test: GetAllManifests_returns_all_manifests()
    [Fact]
    public async Task GetAllManifests_returns_all_manifests()
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
              }
            ]
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri("file:///apps.json") },
            fileSystem: fileSystem);

        var catalog = new Fdc3ModuleCatalog(appDirectory, new MockHostManifestMapper(_hostManifestName).Object);

        var manifests = await catalog.GetAllManifests();

        manifests.Should().HaveCount(3);
    }
}
