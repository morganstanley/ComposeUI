using ModuleLoaderPrototype.Interfaces;
using System.Diagnostics;
using System.Reactive.Subjects;

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

        public ProcessInfo ProcessInfo => new ProcessInfo
        (
            name: Name,
            uiType: UIType.Window,
            uiHint: _mainProcess?.Id.ToString()
        );

        public ExecutableModule(string name, string launchPath)
        {
            Name = name;
            _launchPath = launchPath;
        }

        public Task Initialize()
        {
            var mainProcess = new Process();
            mainProcess.StartInfo.FileName = _launchPath;
            mainProcess.EnableRaisingEvents = true;
            mainProcess.Exited += ProcessExited;
            _mainProcess = mainProcess;
            return Task.CompletedTask;
        }

        public Task Launch()
        {
            _mainProcess?.Start();
            _lifecycleEvents.OnNext(LifecycleEvent.Started(ProcessInfo));
            return Task.CompletedTask;
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, exitRequested));
        }

        public async Task Teardown()
        {
            if (_mainProcess == null)
            {
                _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, true));
                return;
            }
            try
            {
                exitRequested = true;
                var killNecessary = true;

                if (_mainProcess.CloseMainWindow())
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    if (_mainProcess.HasExited)
                    {
                        killNecessary = false;
                    }
                }

                if (killNecessary)
                {
                    _mainProcess.Kill();
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                if (_mainProcess.HasExited)
                {
                    _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, true));
                }
                else
                {
                    _lifecycleEvents.OnNext(LifecycleEvent.StoppingCanceled(ProcessInfo, false));
                }
            }
            finally
            {
                exitRequested = false;
            }
        }
    }
}
