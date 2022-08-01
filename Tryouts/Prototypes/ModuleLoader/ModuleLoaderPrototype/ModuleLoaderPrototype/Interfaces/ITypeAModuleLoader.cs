using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype.Interfaces
{
    internal interface ITypeAModuleLoader
    {
        int StartProcess(LaunchRequest request, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> StopProcess(int processId, CancellationToken cancellationToken = default(CancellationToken));
        IObservable<ProcessRestarted> ProcessRestarted { get; }
    }
}
