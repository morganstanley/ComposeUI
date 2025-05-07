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

using System.Text.Json.Serialization;
using MorganStanley.ComposeUI.ModuleLoader;
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal class ComposeUIHostManifest
{
    [JsonProperty("initialModulePosition")]
    [JsonPropertyOrder(0)]
    public InitialModulePosition? InitialModulePosition { get; set; }

    [JsonProperty("height")]
    [JsonPropertyOrder(1)]
    public double? Height {  get; set; }

    [JsonProperty("width")]
    [JsonPropertyOrder(2)]
    public double? Width { get; set; }

    [JsonProperty("coordinates")]
    [JsonPropertyOrder(3)]
    public Coordinates? Coordinates { get; set; }
}
