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

using ModuleProcessMonitor.Subsystems;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;

namespace ModuleProcessMonitor;

public class Module
{
    public string Name { get; set; }
    public string StartupType { get; set; }
    public string UIType { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
    public string[]? Arguments { get; set; }
    public int? Port { get; set; }
    public string? State { get; set; } = SubsystemState.Stopped;

    public static explicit operator Module(ModuleManifest manifest)
    {
        var module = new Module
        {
            Name = manifest.Name,
            StartupType = manifest.StartupType,
            UIType = manifest.UIType,
            Path = manifest.Path,
            Url = manifest.Url,
            Arguments = manifest.Arguments,
            Port = manifest.Port
        };

        return module;
    }
}
