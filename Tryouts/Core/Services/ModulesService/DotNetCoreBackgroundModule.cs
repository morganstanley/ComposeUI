using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Diagnostics;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService
{
    internal class DotNetCoreBackgroundModule : ModuleBase
    {
        private readonly string _path;
        private Process _mainProcess;

        public DotNetCoreBackgroundModule(string name, Guid instanceId, string path) : base(name, instanceId)
        {
            _path = path;
        }

        public override ProcessInfo ProcessInfo => new ProcessInfo(
            name: Name,
            instanceId: InstanceId,
            uiType: UIType.None,
            uiHint: null
            );

        public override Task Initialize()
        {
            return Task.CompletedTask;
        }

        public override Task Launch()
        {
            _mainProcess = new Process();
            _mainProcess.StartInfo.UseShellExecute = false;

            var location = Path.GetFullPath(_path);
            _mainProcess.StartInfo.FileName = "dotnet";
            _mainProcess.StartInfo.ArgumentList.Add(location);
            _mainProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(location);
            _mainProcess.Start();
            _lifecycleEvents.OnNext(LifecycleEvent.Started(ProcessInfo));
            return Task.CompletedTask;
        }

        public async override Task Teardown()
        {
            if (_mainProcess == null)
            {
                _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, true));
                return;
            }

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
    }
}
