using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class ModuleManifest
    {
        public string Name { get; set; }
        public ModuleType ModuleType { get; set; }
        public string Path { get; set; }
    }
}
