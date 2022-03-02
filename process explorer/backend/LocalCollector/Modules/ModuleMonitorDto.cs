/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities.Modules;

namespace LocalCollector.Modules
{
    public class ModuleMonitorDto
    {
        public SynchronizedCollection<ModuleDto>? CurrentModules { get; set; } = new SynchronizedCollection<ModuleDto>();
        public static ModuleMonitorDto FromAssembly()
        {
            var monduleMonitor = new ModuleMonitorDto();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            lock (assemblies)
            {
                foreach (var assembly in assemblies)
                {
                    var modules = assembly.GetLoadedModules();
                    lock (modules)
                    {
                        foreach (var module in modules)
                        {
                            monduleMonitor?.CurrentModules?.Add(ModuleDto.FromModule(assembly, module));
                        }
                    }
                }
            }
            return monduleMonitor;
        }
    }
}
