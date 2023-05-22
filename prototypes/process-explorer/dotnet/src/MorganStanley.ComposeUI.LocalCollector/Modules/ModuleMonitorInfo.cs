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

using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Modules;

namespace MorganStanley.ComposeUI.ProcessExplorer.LocalCollector.Modules;

public class ModuleMonitorInfo
{
    public SynchronizedCollection<ModuleInfo> CurrentModules { get; internal set; } = new();

    public static ModuleMonitorInfo FromAssembly()
    {
        var moduleMonitor = new ModuleMonitorInfo();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var modules = assembly.GetLoadedModules();
            foreach (var module in modules)
            {
                moduleMonitor.CurrentModules.Add(ModuleInfo.FromModule(assembly, module));
            }
        }

        return moduleMonitor;
    }
}
