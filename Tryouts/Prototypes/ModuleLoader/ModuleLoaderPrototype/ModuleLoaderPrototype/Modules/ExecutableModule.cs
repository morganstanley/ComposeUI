using ModuleLoaderPrototype.Interfaces;
using System.Diagnostics;

namespace ModuleLoaderPrototype.Modules;

internal class ExecutableModule : ModuleBase
{
    private string _launchPath;
    private Process? _mainProcess;
    private bool exitRequested = false;

    public ProcessInfo ProcessInfo => new ProcessInfo
    (
        name: Name,
        uiType: UIType.Window,
        uiHint: _mainProcess?.Id.ToString()
    );

    public ExecutableModule(string name, string launchPath) : base(name)
    {
        _launchPath = launchPath;
    }

    public override Task Initialize()
    {
        var mainProcess = new Process();
        mainProcess.StartInfo.FileName = _launchPath;
        mainProcess.EnableRaisingEvents = true;
        mainProcess.Exited += ProcessExited;
        _mainProcess = mainProcess;
        return Task.CompletedTask;
    }

    public override Task Launch()
    {
        _mainProcess?.Start();
        _lifecycleEvents.OnNext(LifecycleEvent.Started(ProcessInfo));
        return Task.CompletedTask;
    }

    private void ProcessExited(object? sender, EventArgs e)
    {
        _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, exitRequested));
    }

    public async override Task Teardown()
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
