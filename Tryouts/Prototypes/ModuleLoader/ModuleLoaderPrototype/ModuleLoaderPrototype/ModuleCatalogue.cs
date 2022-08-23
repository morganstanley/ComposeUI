using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class ModuleCatalogue : IModuleCatalogue
    {
        public ModuleCatalogue(Dictionary<string, ModuleManifest> modules)
        {
            Modules = modules;
        }

        Dictionary<string, ModuleManifest> Modules { get; } = new Dictionary<string, ModuleManifest>();

        public ModuleManifest GetManifest(string moduleName)
        {
            ModuleManifest? manifest;
            if (!Modules.TryGetValue(moduleName, out manifest))
            {
                throw new Exception("Unknown module name");
            }
            return manifest;
        }
    }
}
