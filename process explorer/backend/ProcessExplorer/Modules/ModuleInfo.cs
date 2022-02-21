/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
using System.Collections.Concurrent;
using System.Reflection;

namespace ProcessExplorer.Entities.Modules
{
    public class ModuleInfo
    {
        public ModuleInfo(Assembly assembly, Module module)
        {
            Name = assembly.GetName().Name;
            Version = module.ModuleVersionId;
            VersionRedirectedFrom = assembly.ManifestModule.ModuleVersionId.ToString(); //module.Assembly.ImageRuntimeVersion;
            PublicKeyToken = assembly.GetName().GetPublicKeyToken(); //module.MetadataToken; 
            Path = module.Assembly.Location;
        }
        public ModuleInfo(string name, Guid version, string versionrf, byte[] publickey, 
            string path,  IEnumerable<CustomAttributeData> dependencies)
        {
            Name = name;
            Version = version;
            VersionRedirectedFrom = versionrf;
            PublicKeyToken = publickey;
            Path = path;
            Dependencies = dependencies;
        }
        public ModuleInfo(string name)
            :this(name, default, default, default, default, default)
        {

        }
        public string? Name { get; protected set; }
        public Guid? Version { get; protected set; }
        public string? VersionRedirectedFrom { get; protected set; }
        public byte[]? PublicKeyToken { get; protected set; }
        public string? Path { get; protected set; }
        public IEnumerable<CustomAttributeData>? Dependencies { get; protected set; }
    }

    public class ModuleMonitor
    {
        public ConcurrentBag<ModuleInfo> currentModules;
        public ModuleMonitor()
        {
            currentModules = new ConcurrentBag<ModuleInfo>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var module in assembly.GetLoadedModules())
                {
                    currentModules.Add(new ModuleInfo(assembly, module));
                }
            }
        }
        public ConcurrentBag<ModuleInfo>? GetModules()
            => currentModules;
    }
}
