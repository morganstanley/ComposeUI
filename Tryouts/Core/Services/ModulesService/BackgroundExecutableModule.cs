using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Diagnostics;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;

internal class BackgroundExecutableModule : ModuleBase
{
    private readonly string _launchPath;
    private Process? _mainProcess;
    private bool _exitRequested = false;

    public override ProcessInfo ProcessInfo => new ProcessInfo
    (
        name: Name,
        instanceId: InstanceId,
        uiType: UIType.None,
        uiHint: null
    );

    public BackgroundExecutableModule(string name, Guid instanceId, string launchPath) : base(name, instanceId)
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
        _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, _exitRequested));
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
            _exitRequested = true;
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
            _exitRequested = false;
        }
    }
}
