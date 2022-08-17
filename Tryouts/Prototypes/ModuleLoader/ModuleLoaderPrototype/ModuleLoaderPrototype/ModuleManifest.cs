using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class ModuleManifest
    {
        public string Name { get; }
        public ModuleType ModuleType { get; }
        public string Path { get; }
    }
}
