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

using ModuleLoaderPrototype.Interfaces;
using System.Diagnostics;
using System.Reactive.Subjects;

namespace ModuleLoaderPrototype;

public class MessageBasedModuleLoader : ITypeBModuleLoader
{

    private Subject<LifecycleEvent> _lifecycleEvents = new Subject<LifecycleEvent>();
    public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents;
    private List<ProcessInfo> _processes = new List<ProcessInfo>();

    private readonly bool _autoRestart;
    public MessageBasedModuleLoader(bool autoRestart)
    {
        _autoRestart = autoRestart;
    }

    public void RequestStartProcess(LaunchRequest request)
    {
        Task.Run(() => StartProcess(request));
    }

    private int StartProcess(LaunchRequest request)
    {
        var process = ProcessLauncher.LaunchProcess(request.path);
        process.EnableRaisingEvents = true;
        process.Exited += HandleProcessExitedUnexpectedly;
        _processes.Add(new ProcessInfo(request.name, process));
        _lifecycleEvents.OnNext(LifecycleEvent.Started(request.name, process.Id));
        return process.Id;
    }

    public async void RequestStopProcess(string name)
    {
        var p = _processes.FirstOrDefault(x => x.Name == name);
        if (p == null)
        {
            throw new Exception("Unknown process name");
        }
        if (await StopProcess(p.Process))
        {
            _processes.Remove(p);
            _lifecycleEvents.OnNext(LifecycleEvent.Stopped(p.Name, p.ProcessId));
        }
        else
        {
            _lifecycleEvents.OnNext(LifecycleEvent.StoppingCanceled(p.Name, p.ProcessId, false));
        }
    }

    private async Task<bool> StopProcess(Process p)
    {
        p.Exited -= HandleProcessExitedUnexpectedly;
        await Task.WhenAny(Task.Run(() => p.CloseMainWindow()), Task.Delay(TimeSpan.FromSeconds(1)));
        if (p.HasExited)
        {
            return true;
        }

        p.Kill();
        if (p.HasExited)
        {
            return true;
        }
        return false;
    }

    private void HandleProcessExitedUnexpectedly(object sender, EventArgs e)
    {
        Process p = (Process)sender;
        var processInfo = _processes.FirstOrDefault(x => x.ProcessId == p.Id);
        p.Exited -= HandleProcessExitedUnexpectedly;
        _lifecycleEvents.OnNext(LifecycleEvent.Stopped(processInfo?.Name ?? String.Empty, p.Id, false));
        var filename = p.StartInfo.FileName;
        _processes.Remove(processInfo);
        if (_autoRestart)
        {
            var pid = StartProcess(new LaunchRequest { path = filename, name = processInfo.Name });
        }
    }
}
