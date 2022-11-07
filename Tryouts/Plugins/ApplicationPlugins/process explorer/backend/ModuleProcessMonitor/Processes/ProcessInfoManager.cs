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

namespace ModuleProcessMonitor.Processes;

public abstract class ProcessInfoManager
{
    private event EventHandler<int>? _sendNewProcess;
    private event EventHandler<int>? _sendTerminatedProcess;
    private event EventHandler<int>? _sendModifiedProcess;
    private SynchronizedCollection<int> _processIds = new();
    private readonly object _locker = new();

    /// <summary>
    /// Sets the events to watch processes. Sets the actions to watch created, terminated and modified processes.
    /// </summary>
    /// <param name="sendModifiedProcessAction"></param>
    /// <param name="sendNewProcessAction"></param>
    /// <param name="sendTerminatedProcessAction"></param>
    public void SetEvents(EventHandler<int> sendModifiedProcessAction,
        EventHandler<int> sendNewProcessAction,
        EventHandler<int> sendTerminatedProcessAction)
    {
        _sendModifiedProcess += sendModifiedProcessAction;
        _sendTerminatedProcess += sendTerminatedProcessAction;
        _sendNewProcess += sendNewProcessAction;
    }

    public bool ContainsId(Process process)
    {
        lock (_locker)
        {
            return _processIds.Contains(process.Id);
        }
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

    public void RemoveProcess(int pid)
    {
        lock (_locker)
        {
            _processIds.Remove(pid);
        }
    }

    public void SetProcessIds(IEnumerable<int> ids)
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

    /// <summary>
    /// Returns the PPID of the given process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public abstract int? GetParentId(Process? process);

    /// <summary>
    /// Returns the memory usage (%) of the given process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public abstract float GetMemoryUsage(Process process);

    /// <summary>
    /// Returns the CPU usage (%) of the given process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public abstract float GetCpuUsage(Process process);

    /// <summary>
    /// Sets the processes to watch.
    /// </summary>
    /// <param name="processes"></param>
    public abstract void SetWatchableProcessList(IEnumerable<ProcessInfoData> processes);

    /// <summary>
    /// Continuously watching created processes.
    /// </summary>
    public abstract void WatchProcesses();

    /// <summary>
    /// Checks if the given process is related to the main process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    private bool IsComposeProcess(object process)
    {
        var proc = process as Process;
        proc?.Refresh();

        if (proc == null)
        {
            return false;
        }

        if (proc.Id == ProcessMonitor.ComposePid ||
            ContainsId(proc.Id))
        {
            return true;
        }

        int? ppid;
        try
        {
            ppid = GetParentId(proc);

            if (ppid == null || ppid == 0)
            {
                return false;
            }

            if (ContainsId((int)ppid))
            {
                return true;
            }

            proc = Process.GetProcessById(Convert.ToInt32(ppid));
            return IsComposeProcess(proc);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Searches for child processes to watch.
    /// </summary>
    /// <param name="processInfo"></param>
    /// <returns></returns>
    public abstract IEnumerable<ProcessInfoData> AddChildProcesses(ProcessInfoData processInfo);

    /// <summary>
    /// Checks if a process belongs to the Compose.
    /// </summary>
    /// <param name="pid"></param>
    public bool CheckIfIsComposeProcess(int pid)
    {
        return IsComposeProcess(Process.GetProcessById(pid));
    }

    public void SendNewProcessUpdate(int pid)
    {
        _sendNewProcess?.Invoke(this, pid);
    }

    /// <summary>
    /// Sends a terminated process information to publish
    /// </summary>
    /// <param name="pid"></param>
    public void SendTerminatedProcessUpdate(int pid)
    {
        _sendTerminatedProcess?.Invoke(this, pid);
    }

    /// <summary>
    /// Sends a modified process information to publish
    /// </summary>
    /// <param name="pid"></param>
    public void SendProcessModifiedUpdate(int pid)
    {
        _sendModifiedProcess?.Invoke(this, pid);
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
