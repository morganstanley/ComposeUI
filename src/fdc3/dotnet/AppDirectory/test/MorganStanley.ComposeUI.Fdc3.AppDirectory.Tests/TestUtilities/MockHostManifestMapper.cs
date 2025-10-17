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
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory.TestUtilities;

internal class MockHostManifestMapper : Mock<IHostManifestMapper>
{
    private readonly string _hostManifestName;

    public MockHostManifestMapper(string hostManifestName)
    {
        _hostManifestName = hostManifestName;

        Setup(_ => _.MapModuleDetails(It.IsAny<Fdc3App>()))
            .Returns(HandleParsing);
    }

    private WebManifestDetails HandleParsing(Fdc3App app)
    {
        var iconSrc = app.Icons?.FirstOrDefault()?.Src;
        var url = new Uri(((WebAppDetails) app.Details).Url, UriKind.Absolute);

        if (app.HostManifests != null
                && app.HostManifests.TryGetValue(_hostManifestName, out var hostManifest))
        {
            var dynamicHostManifest = (dynamic) hostManifest;

            return new WebManifestDetails()
            {
                Url = url,
                IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                InitialModulePosition = dynamicHostManifest.initialModulePosition,
                Height = dynamicHostManifest.height,
                Width = dynamicHostManifest.width,
                Coordinates = new Coordinates()
                {
                    X = dynamicHostManifest.coordinates.x,
                    Y = dynamicHostManifest.coordinates.y
                },
            };
        }

        return new WebManifestDetails()
        {
            Url = url,
            IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
        };
    }
}
