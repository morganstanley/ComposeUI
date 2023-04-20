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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions.Handlers;
using ProcessExplorer.Abstractions.Logging;

namespace ProcessExplorer.Abstractions.Processes;

public abstract class ProcessInfoMonitor : IProcessInfoMonitor
{
    private ProcessCreatedHandler? _processCreatedHandler;
    private ProcessModifiedHandler? _processModifiedHandler;
    private ProcessTerminatedHandler? _processTerminatedHandler;
    private ProcessesModifiedHandler? _processesModifiedHandler;
    private ProcessStatusChangedHandler? _processStatusChangedHandler;
    private readonly ILogger _logger;
    private readonly ObservableCollection<int> _processIds = new();
    private readonly object _locker = new();

    public ProcessInfoMonitor(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
        _processIds.CollectionChanged += ProcessIdsChanged;
    }

    private void ProcessIdsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    if (e.NewItems != null)
                        foreach (int pid in e.NewItems)
                            _processCreatedHandler?.Invoke(pid);

                    break;
                }

            case NotifyCollectionChangedAction.Remove:
                {
                    if (e.OldItems != null)
                        foreach (int pid in e.OldItems)
                            ProcessTerminated(pid);

                    break;
                }
        }
    }

    public void SetHandlers(
        ProcessModifiedHandler processModifiedHandler,
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessCreatedHandler processCreatedHandler,
        ProcessesModifiedHandler processesModifiedHandler,
        ProcessStatusChangedHandler processStatusChangedHandler)
    {
        _processCreatedHandler = processCreatedHandler;
        _processModifiedHandler = processModifiedHandler;
        _processTerminatedHandler = processTerminatedHandler;
        _processStatusChangedHandler = processStatusChangedHandler;
        _processesModifiedHandler = processesModifiedHandler;
    }

    public bool ContainsId(int processId)
    {
        lock (_locker)
        {
            return _processIds.Contains(processId);
        }
    }

    public void AddProcess(int processId)
    {
        lock (_locker)
        {
            if (processId == 0) return;

            _processIds.Add(processId);
        }
    }

    public void RemoveProcessId(int processId)
    {
        lock (_locker)
        {
            _processIds.Remove(processId);
        }
    }

    public void SetProcessIds(
        int mainProcessId,
        ReadOnlySpan<int> processIds)
    {
        lock (_locker)
        {
            foreach (var id in processIds)
            {
                try
                {
                    if (_processIds.Contains(id)) continue;

                    var process = Process.GetProcessById(id);

                    _processIds.Add(id);

                    AddChildProcesses(id, process.ProcessName);
                }
                catch (Exception exception)
                {
                    _logger.ProcessExpected(exception);
                }
            }

            if (mainProcessId != 0 && !_processIds.Contains(mainProcessId))
                _processIds.Add(mainProcessId);
        }
    }

    public ReadOnlySpan<int> GetProcessIds()
    {
        lock (_locker)
        {
            return _processIds.ToArray();
        }
    }

    public void ClearProcessIds()
    {
        lock (_locker)
        {
            _processIds.Clear();
        }
    }

    /// <summary>
    /// Returns the PPID of the given process.
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    public abstract int? GetParentId(int processId, string processName);

    /// <summary>
    /// Returns the memory usage (%) of the given process.
    /// </summary>
    /// <returns></returns>
    public abstract float GetMemoryUsage(int processId, string processName);

    /// <summary>
    /// Returns the CPU usage (%) of the given process.
    /// </summary>
    /// <returns></returns>
    public abstract float GetCpuUsage(int processId, string processName);

    /// <summary>
    /// Continuously watching created processes.
    /// </summary>
    public virtual void WatchProcesses(int mainProcessId)
    {
        if (mainProcessId == 0) return;
        AddProcess(mainProcessId);
        AddChildProcesses(mainProcessId, Process.GetProcessById(mainProcessId).ProcessName);
    }

    /// <summary>
    /// Checks if the given process is related to the main process.
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    private bool IsComposeProcess(int processId)
    {
        //snapshot if the process has already exited
        if (!Process.GetProcesses().Any(p => p.Id == processId)) return false;

        if (ContainsId(processId)) return true;

        var parentProcessId = GetParentId(processId, Process.GetProcessById(processId).ProcessName);

        if (parentProcessId == null || parentProcessId == 0) return false;

        if (ContainsId((int)parentProcessId)) return true;

        return IsComposeProcess(Convert.ToInt32(parentProcessId));
    }

    /// <summary>
    /// Searches for child processes to watch.
    /// </summary>
    public abstract ReadOnlySpan<int> AddChildProcesses(int processId, string? processName);

    /// <summary>
    /// Checks if a process belongs to the Compose.
    /// </summary>
    /// <param name="processId"></param>
    public bool CheckIfIsComposeProcess(int processId)
    {
        return IsComposeProcess(processId);
    }

    /// <summary>
    /// Sends a modified process information to publish
    /// </summary>
    /// <param name="processId"></param>
    public void SendProcessModifiedUpdate(int processId)
    {
        _processModifiedHandler?.Invoke(processId);
    }

    ~ProcessInfoMonitor()
    {
        Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing) { }


    private void ProcessTerminated(int pid)
    {
        if (!TryDeleteProcess(pid)) _logger.CannotTerminateProcessError(pid);
    }

    private bool TryDeleteProcess(int processId)
    {
        try
        {
            _logger.ProcessTerminatedInformation(processId);
            _processStatusChangedHandler?.Invoke(new(processId, Status.Terminated));
            RemoveProcessAfterTimeout(processId);
            return true;
        }
        catch (Exception exception)
        {
            _logger.PpidExpected(processId, exception);
        }

        return false;
    }

    private void RemoveProcessAfterTimeout(int processId)
    {
        var ids = GetProcessIds();
        _processesModifiedHandler?.Invoke(ids);
        _processTerminatedHandler?.Invoke(processId);
    }
}
