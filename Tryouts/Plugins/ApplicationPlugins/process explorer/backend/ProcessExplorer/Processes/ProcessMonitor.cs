// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction.Handlers;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Core.Logging;

namespace ProcessExplorer.Core.Processes;

//TODO(Lilla): Observability changes later.
internal class ProcessMonitor : IProcessMonitor
{
    public int ComposePid { get; private set; }
    public int DelayTime = 60000;
    private readonly ILogger _logger;
    private readonly object _locker = new();
    private bool _disposed = false;

    private readonly ProcessInfoManager? _processInfoManager;

    private ProcessesModifiedHandler? _processesModifiedHandler;
    private ProcessCreatedHandler? _processCreatedHandler;
    private ProcessModifiedHandler? _processModifiedHandler;
    private ProcessTerminatedHandler? _processTerminatedHandler;
    private ProcessStatusChangedHandler? _processStatusChangedHandler;

    public ProcessMonitor(
        ProcessInfoManager? processInfoManager,
        ILogger? logger = null)
        : this(logger)
    {
        _processInfoManager = processInfoManager;
        SetEventsIfTheyAreNotDeclared();
    }

    public ProcessMonitor(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger<IProcessMonitor>.Instance;
    }

    private void ClearList()
    {
        lock (_locker)
        {
            _processInfoManager?.ClearProcessIds();
        }
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        DelayTime = delay * 1000;
    }

    public void SetWatcher()
    {
        Task.Run(() => _processInfoManager?.WatchProcesses());
        _logger.ProcessListIsInitializedDebug();
    }

    public void UnsetWatcher()
    {
        _processInfoManager?.Dispose();
    }

    public void SetComposePid(int pid)
    {
        ComposePid = pid;
        _processInfoManager?.SetComposePid(pid);
    }

    public void SetHandlers(
        ProcessModifiedHandler processModifiedHandler,
        ProcessCreatedHandler processCreatedHandler,
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessesModifiedHandler processesModifiedHandler,
        ProcessStatusChangedHandler processStatusChangedHandler)
    {
        _processCreatedHandler = processCreatedHandler;
        _processTerminatedHandler = processTerminatedHandler;
        _processesModifiedHandler = processesModifiedHandler;
        _processStatusChangedHandler = processStatusChangedHandler;
        _processModifiedHandler = processModifiedHandler;

        SetEventsIfTheyAreNotDeclared();
    }

    private void SetEventsIfTheyAreNotDeclared()
    {
        _processInfoManager?.SetHandlers(ProcessModified, ProcessTerminated, ProcessCreated);
    }

    private void ProcessModified(int pid)
    {
        if (_processModifiedHandler == null) return;
        _processModifiedHandler.Invoke(pid);
    }

    private void ProcessCreated(int pid)
    {
        if (_processCreatedHandler == null) return;
        _processCreatedHandler.Invoke(pid);
    }

    private void ProcessTerminated(int pid)
    {
        if (!TryDeleteProcess(pid)) _logger.LogError("Something");
    }


    public ReadOnlySpan<int> GetProcessIds()
    {
        if (_processInfoManager == null)
        {
            _logger.LogInformation("List is null");
            return null;
        }

        return _processInfoManager.GetProcessIds();
    }

    private void KillProcess(Process process)
    {
        try
        {
            process.Kill();
        }
        catch (Exception exception)
        {
            _logger.KillProcessError(exception);
        }
    }

    public void KillProcessByName(string processName)
    {
        var process = Process.GetProcessesByName(processName)
            .First();

        KillProcessWithChecking(process);
    }

    public void KillProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);

        KillProcessWithChecking(process);
    }

    private bool TryDeleteProcess(int pid)
    {
        try
        {
            _logger.ProcessTerminatedInformation(pid);
            _processStatusChangedHandler?.Invoke(new(pid, Status.Terminated));
            RemoveProcessAfterTimeout(pid);
            return true;
        }
        catch (Exception exception)
        {
            _logger.PpidExpected(pid, exception);
        }

        return false;
    }

    private void RemoveProcessAfterTimeout(int item)
    {
        if (_processInfoManager == null) return;

        Task.Run(() =>
        {
            Task.Delay(DelayTime);
            var ids = _processInfoManager.GetProcessIds();
            _processesModifiedHandler?.Invoke(ids);
            _processTerminatedHandler?.Invoke(item);
        });
    }

    private void KillProcessWithChecking(Process? process)
    {
        if (process == null || _processInfoManager == null) return;
        var isComposeProcess = _processInfoManager.CheckIfIsComposeProcess(process.Id);

        if (isComposeProcess) KillProcess(process);
    }

    public void InitProcesses(ReadOnlySpan<int> pids)
    {
        ClearList();
        _processInfoManager?.SetProcessIds(pids);

        foreach (var pid in pids)
        {
            _processInfoManager?.AddChildProcesses(pid, Process.GetProcessById(pid).ProcessName);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        UnsetWatcher();
        _disposed = true;
    }
}
