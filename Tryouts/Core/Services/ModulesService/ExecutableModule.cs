/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Diagnostics;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;

internal class ExecutableModule : ModuleBase
{
    private string _launchPath;
    private Process? _mainProcess;
    private string[] _arguments;
    private bool _exitRequested = false;

    public override ProcessInfo ProcessInfo => new ProcessInfo
    (
        name: Name,
        instanceId: InstanceId,
        uiType: UIType.Window,
        uiHint: _mainProcess?.MainWindowHandle.ToString()
    );

    public ExecutableModule(string name, Guid instanceId, string launchPath, string[] arguments) : base(name, instanceId)
    {
        _launchPath = launchPath;
        _arguments = arguments;
    }

    public override Task Initialize()
    {
        var mainProcess = new Process();
        mainProcess.StartInfo.FileName = _launchPath;
        mainProcess.EnableRaisingEvents = true;
        mainProcess.Exited += ProcessExited;

        foreach (var argument in _arguments)
        {
            mainProcess.StartInfo.ArgumentList.Add(argument);
        }

        _mainProcess = mainProcess;
        return Task.CompletedTask;
    }

    public override async Task Launch()
    {
        _mainProcess?.Start();
        while (_mainProcess?.MainWindowHandle.ToInt64() == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        _lifecycleEvents.OnNext(LifecycleEvent.Started(ProcessInfo));
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
