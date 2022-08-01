using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype.Interfaces
{
    internal interface ITypeBModuleLoader
    {
        void RequestStartProcess(LaunchRequest request);
        void RequestStopProcess(int pid);
        IObservable<LifecycleEvent> LifecycleEvents { get; }
    }
}
