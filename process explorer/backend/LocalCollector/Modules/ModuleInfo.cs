/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Reflection;

namespace ProcessExplorer.Entities.Modules
{
    public class ModuleInfo
    {
        public ModuleInfo(Assembly assembly, Module module)
            :this()
        {
            Data.Name = assembly.GetName().Name;
            Data.Version = module.ModuleVersionId;
            Data.VersionRedirectedFrom = assembly.ManifestModule.ModuleVersionId.ToString(); 
            Data.PublicKeyToken = assembly?.GetName()?.GetPublicKeyToken(); 
            Data.Path = module.Assembly.Location;
        }

        public ModuleInfo(string name, Guid version, string versionrf, byte[] publickey, 
            string path,  List<CustomAttributeData> information)
            :this()
        {
            Data.Name = name;
            Data.Version = version;
            Data.VersionRedirectedFrom = versionrf;
            Data.PublicKeyToken = publickey;
            Data.Path = path;
            Data.Information = information;
        }

        public ModuleInfo(string name)
            :this(name, default, default, default, default, default)
        {

        }

        ModuleInfo()
            => Data = new ModuleDto();

        public ModuleDto Data { get; set; }
    }

    public class ModuleDto
    {
        public string? Name { get; set; }
        public Guid? Version { get; set; }
        public string? VersionRedirectedFrom { get; set; }
        public byte[]? PublicKeyToken { get; set; }
        public string? Path { get; internal set; }
        public List<CustomAttributeData>? Information { get; set; } = new List<CustomAttributeData>();
    }

    public class ModuleMonitor
    {
        public ModuleMonitorDto Data { get; set; }
        ModuleMonitor()
        {
            Data = new ModuleMonitorDto();
        }
        public ModuleMonitor(bool constless)
            :this()
        {
            if (constless)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var module in assembly.GetLoadedModules())
                    {
                        Data.CurrentModules.Add(new ModuleInfo(assembly, module).Data);
                    }
                }
            }
        }
        public List<ModuleDto>? GetModules()
            => Data.CurrentModules;
    }

    public class ModuleMonitorDto
    {
        public List<ModuleDto>? CurrentModules { get; set; } = new List<ModuleDto>();
    }
}
