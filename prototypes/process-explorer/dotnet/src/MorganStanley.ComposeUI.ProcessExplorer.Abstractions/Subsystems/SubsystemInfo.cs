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

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Subsystems;

//TODO(Lilla): define ProtoContract to derive it implicitly without protoconverthelper
public class SubsystemInfo
{
    public string Name { get; set; } = string.Empty;
    public string StartupType { get; set; } = string.Empty;
    public string UIType { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string[]? Arguments { get; set; }
    public int? Port { get; set; }
    public string State { get; set; } = SubsystemState.Stopped;
    public string? Description { get; set; }
    public bool AutomatedStart { get; set; } = false;

    public static SubsystemInfo FromModule(Module module) => new()
    {
        Name = module.Name,
        StartupType = module.StartupType,
        UIType = module.UIType,
        Path = module.Path,
        Url = module.Url,
        Arguments = module.Arguments,
        Port = module.Port,
        State = module.State,
    };
}
