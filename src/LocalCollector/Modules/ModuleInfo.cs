
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
