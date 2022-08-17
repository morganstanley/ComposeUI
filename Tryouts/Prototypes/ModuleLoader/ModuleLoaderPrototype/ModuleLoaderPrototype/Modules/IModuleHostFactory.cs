using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype.Modules
{
    public interface IModuleHostFactory
    {
        IModule CreateModuleHost(ModuleManifest manifest);
    }
}
