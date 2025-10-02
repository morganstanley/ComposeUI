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
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

/// <summary>
/// Sets the container specific properties to manifest details.
/// </summary>
public interface IHostManifestMapper
{
    /// <summary>
    /// <see cref="Newtonsoft.Json.JsonConverter"/> to enable container specific deserialization for <see cref="Finos.Fdc3.AppDirectory.Fdc3App.HostManifests"/>.
    /// </summary>
    public JsonConverter HostManifestJsonConverter { get; }

    /// <summary>
    /// Maps the container specific information to web properties.
    /// </summary>
    /// <param name="fdc3App"></param>
    /// <returns></returns>
    public ModuleDetails MapModuleDetails (Fdc3App fdc3App);
}
