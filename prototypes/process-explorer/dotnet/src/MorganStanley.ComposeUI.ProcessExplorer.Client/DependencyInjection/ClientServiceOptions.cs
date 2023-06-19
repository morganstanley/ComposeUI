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

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;

namespace MorganStanley.ComposeUI.ProcessExplorer.Client.DependencyInjection;

public class ClientServiceOptions : IOptions<ClientServiceOptions>
{
    public string AssemblyId { get; set; } = Assembly.GetCallingAssembly().GetName().Name ?? string.Empty;
    public int ProcessId { get; set; } = Environment.ProcessId;
    public IServiceCollection? LoadedServices { get; set; }
    public IEnumerable<IConnectionInfo>? Connections { get; set; }
    public IEnumerable<ModuleInfo>? Modules { get; set; }
    public IEnumerable<KeyValuePair<string, string>>? EnvironmentVariables { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5056;
    public ClientServiceOptions Value => this;
}
