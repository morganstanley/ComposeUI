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

namespace MorganStanley.ComposeUI.ModuleLoader;

public sealed class NativeManifestDetails : ModuleDetails
{
    /// <summary>
    /// Url of the module's executable.
    /// </summary>
    public Uri Path { get; set; }

    /// <summary>
    /// Url of the module's icon.
    /// </summary>
    public Uri? Icon { get; init; }

    /// <summary>
    /// Arguments that should be passed to the process when it is being started.
    /// </summary>
    public string[] Arguments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Environment variables that should be set for the given module.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();
}
