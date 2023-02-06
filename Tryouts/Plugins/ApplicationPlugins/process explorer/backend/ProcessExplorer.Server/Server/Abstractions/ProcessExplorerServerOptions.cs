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

using ModuleProcessMonitor.Processes;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Subsystems;

namespace ProcessExplorer.Server.Server.Abstractions;

public class ProcessExplorerServerOptions
{
    public bool EnableProcessExplorer { get; set; }
    public IEnumerable<KeyValuePair<Guid, Module>>? Modules { get; set; }
    public IEnumerable<ProcessInformation>? Processes { get; set; }
    public int? MainProcessID { get; set; }
    public int? Port { get; set; }
    public string? Host { get; set; }
    public ISubsystemLauncher? SubsystemLauncher { get; set; }
    public ISubsystemLauncherCommunicator? SubsystemLauncherCommunicator { get; set; }
}
