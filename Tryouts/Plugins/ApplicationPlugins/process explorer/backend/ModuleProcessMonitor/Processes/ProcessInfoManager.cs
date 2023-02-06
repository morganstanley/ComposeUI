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
using ProcessExplorer.Abstraction.Handlers;

namespace ProcessExplorer.Abstraction.Processes;

public abstract class ProcessInfoManager : IDisposable
{
    private ProcessCreatedHandler? _processCreatedHandler;
    private ProcessModifiedHandler? _processModifiedHandler;
    private ProcessTerminatedHandler? _processTerminatedHandler;

    private readonly ObservableCollection<int> _processIds = new();
    private readonly object _locker = new();
    private int _composePid;

    public ProcessInfoManager()
    {
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
                            _processTerminatedHandler?.Invoke(pid);

                    break;
                }
        }
    }

    public void SetHandlers(
        ProcessModifiedHandler processModifiedHandler,
        ProcessTerminatedHandler processTerminatedHandler,
        ProcessCreatedHandler processCreatedHandler)
    {
        _processCreatedHandler = processCreatedHandler;
        _processModifiedHandler = processModifiedHandler;
        _processTerminatedHandler = processTerminatedHandler;
    }

    public void SetComposePid(int pid)
    {
        _composePid = pid;
    }

    public bool ContainsId(int pid)
    {
        lock (_locker)
        {
            return _processIds.Contains(pid);
        }
    }

    public void AddProcess(int pid)
    {
        lock (_locker)
        {
            _processIds.Add(pid);
        }
    }

    public void RemoveProcessId(int pid)
    {
        lock (_locker)
        {
            _processIds.Remove(pid);
        }
    }

    public void SetProcessIds(ReadOnlySpan<int> ids)
    {
        lock (_locker)
        {
            foreach (var id in ids)
            {
                if (_processIds.Contains(id)) continue;
                _processIds.Add(id);
            }
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
    /// <param name="process"></param>
    /// <returns></returns>
    public abstract float GetMemoryUsage(int id, string processName);

    /// <summary>
    /// Returns the CPU usage (%) of the given process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public abstract float GetCpuUsage(int id, string processName);

    /// <summary>
    /// Continuously watching created processes.
    /// </summary>
    public abstract void WatchProcesses();

    /// <summary>
    /// Checks if the given process is related to the main process.
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    private bool IsComposeProcess(int processId)
    {
        //snapshot if the process has already exited
        if (!Process.GetProcesses().Any(p => p.Id == processId)) return false;

        if (processId == _composePid || ContainsId(processId)) return true;

        var ppid = GetParentId(processId, Process.GetProcessById(processId).ProcessName);

        if (ppid == null || ppid == 0) return false;

        if (ContainsId((int)ppid)) return true;

        return IsComposeProcess(Convert.ToInt32(ppid));
    }

    /// <summary>
    /// Searches for child processes to watch.
    /// </summary>
    /// <param name="processInfo"></param>
    /// <returns></returns>
    public abstract ReadOnlySpan<int> AddChildProcesses(int pid, string? processName);

    /// <summary>
    /// Checks if a process belongs to the Compose.
    /// </summary>
    /// <param name="pid"></param>
    public bool CheckIfIsComposeProcess(int pid)
    {
        return IsComposeProcess(pid);
    }

    public void SendNewProcessUpdate(int pid)
    {
        _processCreatedHandler?.Invoke(pid);
    }

    /// <summary>
    /// Sends a terminated process information to publish
    /// </summary>
    /// <param name="pid"></param>
    public void SendTerminatedProcessUpdate(int pid)
    {
        _processTerminatedHandler?.Invoke(pid);
    }

    /// <summary>
    /// Sends a modified process information to publish
    /// </summary>
    /// <param name="pid"></param>
    public void SendProcessModifiedUpdate(int pid)
    {
        _processModifiedHandler?.Invoke(pid);
    }

    ~ProcessInfoManager()
    {
        Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing) { }
}
