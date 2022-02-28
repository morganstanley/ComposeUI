/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Reflection;

namespace ProcessExplorer.Entities.Modules
{
    public class ModuleDto
    {
        #region Properties
        public string? Name { get; set; }
        public Guid? Version { get; set; }
        public string? VersionRedirectedFrom { get; set; }
        public byte[]? PublicKeyToken { get; set; }
        public string? Path { get; internal set; }
        public SynchronizedCollection<CustomAttributeData>? Information { get; set; } = new SynchronizedCollection<CustomAttributeData>();
        #endregion

        public static ModuleDto FromModule(Assembly assembly, Module module)
        {
            return new ModuleDto()
            {
                Name = assembly.GetName().Name,
                Version = module.ModuleVersionId,
                VersionRedirectedFrom = assembly.ManifestModule.ModuleVersionId.ToString(),
                PublicKeyToken = assembly?.GetName()?.GetPublicKeyToken(),
                Path = module.Assembly.Location
            };
        }

        public static ModuleDto FromProperties(string name, Guid version, string versionrf, byte[] publickey,
            string path, SynchronizedCollection<CustomAttributeData> information)
        {
            return new ModuleDto()
            {
                Name = name,
                Version = version,
                VersionRedirectedFrom = versionrf,
                PublicKeyToken = publickey,
                Path = path,
                Information = information
            };
        }
    }
    public class ModuleMonitorDto
    {
        public SynchronizedCollection<ModuleDto>? CurrentModules { get; set; } = new SynchronizedCollection<ModuleDto>();
        public static ModuleMonitorDto FromAssembly()
        {
            var monduleMonitor = new ModuleMonitorDto();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var module in assembly.GetLoadedModules())
                {
                    monduleMonitor.CurrentModules.Add(ModuleDto.FromModule(assembly, module));
                }
            }
            return monduleMonitor;
        }
    }
}
