using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype
{
    public struct ProcessRestarted
    {
        public int oldPid;
        public int newPid;
    }
}
