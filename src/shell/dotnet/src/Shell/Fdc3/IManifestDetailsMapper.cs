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
    /// Defines a contract for mapping FDC3 app and manifest data to <see cref="ModuleDetails"/>.
    /// </summary>
    public interface IManifestDetailsMapper
    {
        /// <summary>
        /// Maps the given FDC3 app and ComposeUI host manifest to a <see cref="ModuleDetails"/> instance.
        /// </summary>
        /// <param name="fdc3App">The FDC3 app to map.</param>
        /// <param name="composeUIHostManifest">The ComposeUI host manifest, or null if not present.</param>
        /// <param name="iconSrc">The icon source URI as a string, or null.</param>
        /// <returns>A mapped <see cref="ModuleDetails"/> instance.</returns>
        ModuleDetails Map(Fdc3App fdc3App, ComposeUIHostManifest? composeUIHostManifest, string? iconSrc);
    }
}

