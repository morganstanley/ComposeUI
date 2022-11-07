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
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Logging;

namespace ModuleProcessMonitor.Processes;

//TODO(Lilla): needs to handle when observability sends messages to them so new communicator
internal class ProcessMonitor : IProcessMonitor
{
    public static int ComposePid { get; set; }
    public static int DelayTime = 60000;
    private readonly ProcessMonitorInfo _processMonitorInfo = new();
    private readonly ProcessInfoManager? _processInfoManager;
    private readonly ILogger<IProcessMonitor> _logger;
    private readonly object _locker = new();
    public event EventHandler<int>? _processTerminated;
    public event EventHandler<ProcessInfoData>? _processCreated;
    public event EventHandler<ProcessInfoData>? _processModified;
    public event EventHandler<SynchronizedCollection<ProcessInfoData>>? _processesModified;

    public ProcessMonitor(ProcessInfoManager? processInfoManager,
        ILogger<IProcessMonitor>? logger = null)
        : this(logger)
    {
        _processInfoManager = processInfoManager;
        SetEventsIfTheyAreNotDeclared();
    }

    public ProcessMonitor(ILogger<IProcessMonitor>? logger = null)
    {
        _logger = logger ?? NullLogger<IProcessMonitor>.Instance;
    }

    private void ClearList()
    {
        lock (_locker)
        {
            _processMonitorInfo.Processes.Clear();
        }
    }

    public void SetDeadProcessRemovalDelay(int delay)
    {
        DelayTime = delay * 1000;
    }

    public void SetWatcher()
    {
        lock (_locker)
        {
            if (_processMonitorInfo.Processes.Any())
            {
                _processInfoManager?.SetWatchableProcessList(_processMonitorInfo.Processes);
            }
        }
        _processInfoManager?.WatchProcesses();
        _logger.ProcessListIsInitializedDebug();
    }

    public void UnsetWatcher()
    {
        _processInfoManager?.Dispose();
    }

    public void SetComposePid(int pid)
    {
        ComposePid = pid;
    }

    private void SetEventsIfTheyAreNotDeclared()
    {
        _processInfoManager?.SetEvents(SendModifiedProcess, SendNewProcess, SendTerminatedProcess);
    }

    public SynchronizedCollection<ProcessInfoData> GetProcesses()
    {
        lock (_locker)
        {
            return _processMonitorInfo.Processes;
        }
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
            var terminatedProcess = GetProcessInfoData(pid);

            if (terminatedProcess != null)
            {
                _logger.ProcessTerminatedInformation(pid);
                ModifyStatusToTerminated(terminatedProcess);
                RemoveProcessAfterTimeout(terminatedProcess);
                return true;
            }
        }
        catch (Exception exception)
        {
            _logger.PpidExpected(pid, exception);
        }

        return false;
    }

    private void ModifyStatusToTerminated(ProcessInfoData processToModify)
    {
        lock (_locker)
        {
            var index = _processMonitorInfo.Processes.IndexOf(processToModify);
            _processMonitorInfo.Processes[index].ProcessStatus = Status.Terminated.ToStringCached();
            _processMonitorInfo.Processes[index].ProcessorUsage = 0;
            _processMonitorInfo.Processes[index].PhysicalMemoryUsageBit = 0;
            _processMonitorInfo.Processes[index].VirtualMemorySize = 0;
            _processMonitorInfo.Processes[index].PrivateMemoryUsage = 0;
            _processMonitorInfo.Processes[index].Threads = new SynchronizedCollection<ProcessThreadInfo>();
            _processModified?.Invoke(this, _processMonitorInfo.Processes[index]);
        }
    }

    private void RemoveProcessAfterTimeout(ProcessInfoData item)
    {
        Task.Run(async () =>
        {
            await Task.Delay(DelayTime);
            var terminatedProcess = GetProcessInfoData((int)item.PID!);
            if (terminatedProcess != null)
            {
                lock (_locker)
                {
                    var indexOfTerminatedProcess = _processMonitorInfo.Processes.IndexOf(terminatedProcess);

                    _processMonitorInfo.Processes.RemoveAt(indexOfTerminatedProcess);

                    _processesModified?.Invoke(this, _processMonitorInfo.Processes);
                    _processTerminated?.Invoke(this, Convert.ToInt32(item.PID));
                }
            }
        });
    }

    private void SendNewProcess(object? sender, int processId)
    {
        var processInfo = ProcessCreated(Process.GetProcessById(processId));

        if (processInfo == null)
        {
            return;
        }

        if (!AlreadyAdded(processId))
        {
            lock (_locker)
            {
                _processMonitorInfo.Processes.Add(processInfo.ProcessInfo);
            }

            _logger.ProcessCreatedInformation(processId);
        }

        _processCreated?.Invoke(this, processInfo.ProcessInfo);
    }

    private void SendTerminatedProcess(object? sender, int pid)
    {
        if (!TryDeleteProcess(pid))
        {
            _logger.ProcessNotFoundWarning(pid);
        }
    }

    private void SendModifiedProcess(object? sender, int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            process.Refresh();

            if (process.Id <= 0 ||
                _processInfoManager == null) return;

            var processInfo = new ProcessInformation(process);
            ProcessInformation.SetProcessInfoData(processInfo, _processInfoManager);

            if (processInfo.ProcessInfo.PID == null)
            {
                return;
            }

            var existedProcessInfo = GetProcessInfoData((int)processInfo.ProcessInfo.PID!);

            if (existedProcessInfo == null) return;

            lock (_locker)
            {
                var index = Convert.ToInt32(_processMonitorInfo.Processes
                .IndexOf(existedProcessInfo));

                ModifyElement(index, processInfo.ProcessInfo);
            }

            _processModified?.Invoke(this, existedProcessInfo);

            _logger.ProcessModifiedDebug(pid);
        }
        catch (Exception exception)
        {
            _logger.ModifiableProcessExpected(pid, exception);
        }
    }

    private bool AlreadyAdded(int pid)
    {
        lock (_locker)
        {
            var isExists = _processMonitorInfo.Processes
                .Any(p => p.PID == pid);

            return isExists;
        }
    }

    //It is used in lock statements.
    private void ModifyElement(int index, ProcessInfoData processInfo)
    {
        if (index == -1) return;

        _processMonitorInfo.Processes[index].ProcessStatus = processInfo.ProcessStatus;
        _processMonitorInfo.Processes[index].PhysicalMemoryUsageBit = processInfo.PhysicalMemoryUsageBit;
        _processMonitorInfo.Processes[index].ProcessorUsage = processInfo.ProcessorUsage;
        _processMonitorInfo.Processes[index].ProcessorUsageTime = processInfo.ProcessorUsageTime;
        _processMonitorInfo.Processes[index].ProcessPriorityClass = processInfo.ProcessPriorityClass;
        _processMonitorInfo.Processes[index].PriorityLevel = processInfo.PriorityLevel;
        _processMonitorInfo.Processes[index].Threads = processInfo.Threads;
        _processMonitorInfo.Processes[index].VirtualMemorySize = processInfo.VirtualMemorySize;
        _processMonitorInfo.Processes[index].PrivateMemoryUsage = processInfo.PrivateMemoryUsage;
        _processMonitorInfo.Processes[index].MemoryUsage = processInfo.MemoryUsage;
        _processMonitorInfo.Processes[index].StartTime = processInfo.StartTime;
    }

    private void KillProcessWithChecking(Process? process)
    {
        lock (_locker)
        {
            var isComposeProcess = _processMonitorInfo.Processes
                .Any(proc =>
                    proc.PID == process?.Id);

            if (process != null &&
                isComposeProcess)
            {
                KillProcess(process);
            }
        }
    }

    private ProcessInfoData? GetProcessInfoData(int pid)
    {
        lock (_locker)
        {
            var processInfoData = _processMonitorInfo.Processes
                .FirstOrDefault(p => p.PID == pid);

            return processInfoData;
        }
    }

    public void AddProcessInfo(ProcessInfoData processInfo)
    {
        lock (_locker)
        {
            var existedProcessInfo = _processMonitorInfo.Processes
                .FirstOrDefault(p =>
                    p.PID == processInfo.PID);


            if (existedProcessInfo == null)
            {
                _processMonitorInfo.Processes.Add(processInfo);
            }
            else
            {
                var index = _processMonitorInfo.Processes
                    .IndexOf(existedProcessInfo);

                ModifyElement(index, processInfo);
            }

            RefreshWatchedProcessList(processInfo);
        }
    }

    public void InitProcesses(IEnumerable<ProcessInfoData> processInfoDatas)
    {
        ClearList();

        var processes = processInfoDatas.ToList();
        lock (_locker)
        {
            foreach (var process in CollectionsMarshal.AsSpan(processes))
            {
                if (process == null) continue;
                _processMonitorInfo.Processes.Add(process);
                _processInfoManager?.AddChildProcesses(process);
            }

            _processesModified?.Invoke(this, _processMonitorInfo.Processes);
        }
    }

    private void RefreshWatchedProcessList(ProcessInfoData processInfo)
    {
        var children = _processInfoManager?
            .AddChildProcesses(processInfo)
            .ToList();

        var pidComparer = new ProcessInfoDataComparer();

        lock (_locker)
        {
            foreach (var child in CollectionsMarshal.AsSpan(children))
            {
                if (!_processMonitorInfo.Processes.Contains(child, pidComparer))
                {
                    _processMonitorInfo.Processes.Add(child);
                }
                else
                {
                    var index = _processMonitorInfo
                        .Processes.IndexOf(child);

                    ModifyElement(index, child);
                }
            }

            //Refreshes the list to watch
            _processInfoManager?.SetWatchableProcessList(_processMonitorInfo.Processes);
        }
    }

    /// <summary>
    /// Creates a processInfoData object.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    private ProcessInformation? ProcessCreated(Process process)
    {
        if (_processInfoManager == null) return null;

        var processInfo = ProcessInformation
            .GetProcessInfoWithCalculatedData(process, _processInfoManager);

        return processInfo;
    }
}
