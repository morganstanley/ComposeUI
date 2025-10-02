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

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal sealed partial class ComposeUIHostManifestMapper
{
    /// <summary>
    /// Maps web FDC3 app and manifest details to <see cref="WebManifestDetails"/>.
    /// </summary>
    internal class WebManifestDetailsMapper : IManifestDetailsMapper
    {
        /// <inheritdoc />
        public ModuleDetails Map(Fdc3App fdc3App, ComposeUIHostManifest? composeUIHostManifest, string? iconSrc)
        {
            var webDetails = (WebAppDetails)fdc3App.Details;
            var url = new Uri(webDetails.Url, UriKind.Absolute);

            return new WebManifestDetails
            {
                Url = url,
                IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                InitialModulePosition = composeUIHostManifest?.InitialModulePosition,
                Height = composeUIHostManifest?.Height,
                Width = composeUIHostManifest?.Width,
                Coordinates = composeUIHostManifest?.Coordinates,
            };
        }
    }
}

