using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public class ModuleDescription
    {
        public ModuleDescription(string name, string path, ModuleType moduleType, string description = "")
        {
            Name = name;
            Description = description;
            Path = path;
            ModuleType = moduleType;
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Path { get; private set; }
        public ModuleType ModuleType { get; private set; }
    }
}
