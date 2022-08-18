using ModuleLoaderPrototype.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace ModuleLoaderPrototype.Modules
{
    internal class ExecutableModule : IModule
    {
        private string _launchPath;
        private Process? _mainProcess;
        private bool exitRequested = false;

        private Subject<LifecycleEvent> _lifecycleEvents = new Subject<LifecycleEvent>();
        public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents;

        public string Name { get; private set; }

        public ExecutableModule(string name, string launchPath)
        {
            Name = name;
            _launchPath = launchPath;
        }

        public void Initialize()
        {
            var mainProcess = new Process();
            mainProcess.StartInfo.FileName = _launchPath;
            mainProcess.EnableRaisingEvents = true;
            mainProcess.Exited += ProcessExited;
            _mainProcess = mainProcess;
        }

        public void Launch()
        {
            _mainProcess?.Start();
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            _lifecycleEvents.OnNext(LifecycleEvent.Stopped(Name, _mainProcess.Id, exitRequested));
        }

        public void Teardown()
        {
            try
            {
                exitRequested = true;

            }
            finally
            {
                exitRequested = false;
            }
        }

    }
}
