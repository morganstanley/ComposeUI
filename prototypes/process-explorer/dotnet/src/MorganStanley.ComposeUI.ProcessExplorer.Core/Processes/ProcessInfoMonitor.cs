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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Logging;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;

namespace MorganStanley.ComposeUI.ProcessExplorer.Core.Processes;

[ExcludeFromCodeCoverage]
public abstract class ProcessInfoMonitor : IProcessInfoMonitor
{
    private readonly ILogger _logger;
    private readonly ObservableCollection<int> _processIds = new();
    private readonly object _processIdsLocker = new();
    private readonly Subject<KeyValuePair<int, ProcessStatus>> _processIdsSubject = new();

    public IObservable<KeyValuePair<int, ProcessStatus>> ProcessIds => _processIdsSubject;


    protected ProcessInfoMonitor(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public bool ContainsId(int processId)
    {
        lock (_processIdsLocker)
        {
            return _processIds.Contains(processId);
        }
    }

    public void AddProcess(int processId)
    {
        lock (_processIdsLocker)
        {
            if (processId == 0)
            {
                return;
            }

            _processIds.Add(processId);
            _processIdsSubject.OnNext(new(processId, ProcessStatus.Running));
        }
    }

    public void RemoveProcessId(int processId)
    {
        lock (_processIdsLocker)
        {
            var removedProcessId = _processIds.Remove(processId);
            if (!removedProcessId)
            {
                return;
            }

            _processIdsSubject.OnNext(new(processId, ProcessStatus.Terminated));
            _logger.ProcessTerminatedInformation(processId);
        }
    }

    public void SetProcessIds(
        int mainProcessId,
        ReadOnlySpan<int> processIds)
    {
        lock (_processIdsLocker)
        {
            foreach (var id in processIds)
            {
                try
                {
                    if (_processIds.Contains(id))
                    {
                        continue;
                    }

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
            {
                _processIds.Add(mainProcessId);
            }
        }
    }

    public ReadOnlySpan<int> GetProcessIds()
    {
        lock (_processIdsLocker)
        {
            return _processIds.ToArray();
        }
    }

    public void ClearProcessIds()
    {
        lock (_processIdsLocker)
        {
            _processIds.Clear();
        }
    }

    /// <summary>
    /// Returns the PPID of the given process.
    /// </summary>
    /// <param name="processId"></param>
    /// <param name="processName"></param>
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
        lock (_processIdsLocker)
        {
            ClearProcessIds();
            if (mainProcessId == 0) return;
            AddProcess(mainProcessId);
            AddChildProcesses(mainProcessId, Process.GetProcessById(mainProcessId).ProcessName);
        }
    }

    /// <summary>
    /// Checks if the given process is related to the main process.
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    private bool IsComposeProcess(int processId)
    {
        //snapshot if the process has already exited
        if (Process.GetProcessById(processId) == null)
        {
            return false;
        }

        if (ContainsId(processId))
        {
            return true;
        }

        var process = Process.GetProcessById(processId);
        if (process.Id == 0)
        {
            return false;
        }

        var parentProcessId = GetParentId(processId, process.ProcessName);

        if (parentProcessId == null || parentProcessId == 0)
        {
            return false;
        }

        if (ContainsId((int)parentProcessId))
        {
            return true;
        }

        return IsComposeProcess(Convert.ToInt32(parentProcessId));
    }

    /// <summary>
    /// Searches for child processes to watch.
    /// </summary>
    public abstract ReadOnlySpan<int> AddChildProcesses(int processId, string? processName);

    public abstract void StopWatchingProcesses();

    /// <summary>
    /// Checks if a process belongs to the Compose.
    /// </summary>
    /// <param name="processId"></param>
    public bool CheckIfIsComposeProcess(int processId)
    {
        try
        {
            return IsComposeProcess(processId);
        }
        catch (Exception exception)
        {
            _logger.ProcessExpected(exception);
            return false;
        }
    }

    /// <summary>
    /// Sends a modified process information to publish
    /// </summary>
    /// <param name="processId"></param>
    public void ProcessModifiedUpdate(int processId)
    {
        lock (_processIdsLocker)
        {
            if (!_processIds.Contains(processId)) return;
            _processIdsSubject.OnNext(new(processId, ProcessStatus.Modified));
        }
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
}
