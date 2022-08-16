using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class ModuleCatalogue
    {
        IReadOnlyDictionary<string, ModuleDescription> Modules { get; } = new Dictionary<string, ModuleDescription>();
    }
}
