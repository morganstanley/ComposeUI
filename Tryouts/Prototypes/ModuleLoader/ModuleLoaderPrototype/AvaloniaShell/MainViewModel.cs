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

using ModuleLoaderPrototype;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace AvaloniaShell;

internal class MainViewModel : ReactiveObject
{

    private readonly MessageBasedModuleLoader _moduleLoader;


    public MainViewModel()
    {
        _moduleLoader = new MessageBasedModuleLoader(false);
        _moduleLoader.LifecycleEvents.Subscribe(HandleAppLifecycleEvent);
    }

    private string _app1Path = string.Empty;
    public string App1Path
    {
        get => _app1Path;
        set => this.RaiseAndSetIfChanged(ref _app1Path, value);
    }

    private string _app2Path = string.Empty;
    public string App2Path
    {
        get => _app2Path;
        set => this.RaiseAndSetIfChanged(ref _app2Path, value);
    }

    private int? _pid1;
    public int? Pid1
    {
        get => _pid1;
        set => this.RaiseAndSetIfChanged(ref _pid1, value);
    }

    private int? _pid2;
    public int? Pid2
    {
        get => _pid2;
        set => this.RaiseAndSetIfChanged(ref _pid2, value);
    }

    public void StartApp1()
    {
        _moduleLoader.RequestStartProcess(new LaunchRequest() { name = "app1", path = _app1Path });
    }
    public void StopApp1()
    {
        _moduleLoader.RequestStopProcess("app1");
    }

    public void StartApp2()
    {
        _moduleLoader.RequestStartProcess(new LaunchRequest() { name = "app2", path = _app2Path });
    }
    public void StopApp2()
    {
        _moduleLoader.RequestStopProcess("app2");
    }

    private async void HandleAppLifecycleEvent(LifecycleEvent lifecycleEvent)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        switch (lifecycleEvent.eventType)
        {
            case LifecycleEventType.Started:
                await SetPid(lifecycleEvent.name, lifecycleEvent.pid);
                break;
            case LifecycleEventType.Stopped:
                await SetPid(lifecycleEvent.name, null);
                break;
        }
    }

    private async Task SetPid(string name, int? pid)
    {
        if (name == "app1")
        {
            Pid1 = pid;
        }
        else if (name == "app2")
        {
            Pid2 = pid;
        }
    }
}
