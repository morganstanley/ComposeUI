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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Logging;

namespace ModuleProcessMonitor.Processes;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class ProcessInfoGeneratorWindows : ProcessInfoManager
{
    private readonly ILogger<ProcessInfoManager> _logger;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _cpuPerformanceCounters;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _memoryPerformanceCounters;
    private ManagementEventWatcher? _watcher;
    private bool _disposed = false;
    private readonly object _lock = new();
    public ProcessInfoGeneratorWindows(ILogger<ProcessInfoManager>? logger)
    {
        _logger = logger ?? NullLogger<ProcessInfoGeneratorWindows>.Instance;
        _cpuPerformanceCounters = new();
        _memoryPerformanceCounters = new();
    }

    public ProcessInfoGeneratorWindows(EventHandler<int> sendNewProcess,
        EventHandler<int> sendTerminatedProcess, EventHandler<int> sendModifiedProcess,
        ILogger<ProcessInfoManager>? logger = null)
        : this(logger)
    {
        base.SetEvents(sendModifiedProcess, sendNewProcess, sendTerminatedProcess);
    }

    public override int? GetParentId(Process? process)
    {
        if (process == null) return null;

        int ppid = 0;

        using var mo =
            new ManagementObject(
                $"win32_process.handle='{process.Id}'");
        try
        {
            mo.Get();
            ppid = Convert.ToInt32(mo["ParentProcessId"]);
        }
        catch (Exception exception)
        {
            if (process.Id > 0)
            {
                _logger.ManagementObjectPpidExpected(process.Id, exception);
            }
        }

        return ppid;
    }

    public override float GetMemoryUsage(Process process)
    {
        int memsize;

        var memoryPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(process.Id, _ =>
        {
            return new PerformanceCounter()
            {
                CategoryName = "Process",
                CounterName = "Working Set - Private",
                InstanceName = process.ProcessName,
            };
        });

        memsize = Convert.ToInt32(memoryPerformanceCounter.NextValue()) / Convert.ToInt32(1024) / Convert.ToInt32(1024);

        return (float)((memsize / GetTotalMemoryInMb()) * 100);
    }

    private static double GetTotalMemoryInMb()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
        return Convert.ToDouble(installedMemory) / 1048576.0;
    }

    public override float GetCpuUsage(Process process)
    {
        var cpuPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(
            process.Id,
            _ =>
            {
                return new PerformanceCounter(
                            "Process",
                            "% Processor Time",
                            process.ProcessName,
                            true);
            });

        var processCpuUsage = cpuPerformanceCounter.NextValue();

        return processCpuUsage / Environment.ProcessorCount;
    }

    public SynchronizedCollection<ProcessInfoData> GetChildProcesses(Process process)
    {
        var children = new SynchronizedCollection<ProcessInfoData>();

        var mos = new ManagementObjectSearcher(
            string.Format(
                "Select * From Win32_Process Where ParentProcessID={0} Or ProcessID={0}",
                process.Id));

        foreach (var objectCollection in mos.Get())
        {
            var managementObject = (ManagementObject)objectCollection;
            try
            {
                var childProcess = Process.GetProcessById(Convert.ToInt32(managementObject["ProcessID"]));

                var childProcessInfo = new ProcessInformation(childProcess);

                ProcessInformation.SetProcessInfoData(childProcessInfo, this);

                children.Add(childProcessInfo.ProcessInfo);
            }
            catch (Exception exception)
            {
                _logger.ChildProcessExpected(exception);
            }
        }

        return children;
    }

    private static List<int> GetProcessIds(IEnumerable<ProcessInfoData> processes)
    {
        var processIds = new List<int>();

        foreach (var process in processes)
        {
            if (process.PID != null)
            {
                processIds.Add(Convert.ToInt32(process.PID));
            }
        }

        return processIds;
    }

    public override void SetWatchableProcessList(IEnumerable<ProcessInfoData> processes)
    {
        Thread.Sleep(5000);
        SetProcessIds(GetProcessIds(processes));
    }

    public override void WatchProcesses()
    {
        const string wmiQuery = "Select * From __InstanceOperationEvent Within 1 " +
                                "Where TargetInstance ISA 'Win32_Process' ";
        try
        {
            var scope = new ManagementScope(@"\\.\root\CIMV2");
            scope.Connect();
            _watcher = new ManagementEventWatcher(scope, new EventQuery(wmiQuery));
            _watcher.EventArrived += WmiEventHandler;
            _watcher.Start();
        }
        catch (Exception exception)
        {
            _logger.WatcherInitializationError(exception);
        }
    }

    public override IEnumerable<ProcessInfoData> AddChildProcesses(ProcessInfoData processInfo)
    {
        lock (_lock)
        {
            if (!ContainsId((int)processInfo.PID!))
            {
                AddProcess((int)processInfo.PID!);
            }
        }

        var processes = new List<ProcessInfoData>
        {
            processInfo
        };

        var pidComparer = new ProcessInfoDataComparer();

        try
        {
            var process = Process.GetProcessById((int)processInfo.PID!);
            var children = GetChildProcesses(process).ToList();

            foreach (var child in CollectionsMarshal.AsSpan(children))
            {
                if (child.PID == process.Id)
                {
                    continue;
                }

                processes.Add(child);

                SendProcessIfItsComposeProcess((int)child.PID!);

                var childrenOfChild = AddChildProcesses(child)
                    .ToList();

                foreach (var cChild in CollectionsMarshal.AsSpan(childrenOfChild))
                {
                    if (processes.Contains(cChild, pidComparer))
                    {
                        continue;
                    }

                    processes.Add(cChild);
                    SendProcessIfItsComposeProcess((int)cChild.PID!);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.PpidExpected((int)processInfo.PID!, exception);
        }

        return processes;
    }

    private void SendProcessIfItsComposeProcess(int pid)
    {
        lock (_lock)
        {
            if (!CheckIfIsComposeProcess(pid)) return;

            var alreadyAdded = ContainsId(pid);
            if (alreadyAdded) return;

            AddProcess(pid);
        }

        SendNewProcessUpdate(pid);
    }

    private void WmiEventHandler(object sender, EventArrivedEventArgs e)
    {
        var pid = Convert.ToInt32(
            ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

        var wclass = (e.NewEvent).SystemProperties["__Class"]
            .Value
            .ToString();

        if (wclass == null) return;

        try
        {
            switch (wclass)
            {
                case "__InstanceCreationEvent":
                    InstanceCreated(pid);
                    break;

                case "__InstanceDeletionEvent":
                    InstanceDeleted(pid);
                    break;

                case "__InstanceModificationEvent":
                    InstanceModified(pid);
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.InstanceEventExpected(exception);
        }
    }

    private void InstanceModified(int pid)
    {
        lock (_lock)
        {
            var isExists = ContainsId(pid);
            if (isExists)
            {
                SendProcessModifiedUpdate(pid);
            }
        }
    }

    private void InstanceCreated(int pid)
    {
        var process = GetProcessIfPidExists(pid);

        if (process == null) return;

        lock (_lock)
        {
            if (!CheckIfIsComposeProcess(pid)) return;

            AddProcess(pid);
        }

        SendNewProcessUpdate(pid);
    }

    private void InstanceDeleted(int pid)
    {
        _cpuPerformanceCounters.TryRemove(pid, out var cpuPerf);
        cpuPerf?.Close();
        cpuPerf?.Dispose();

        _memoryPerformanceCounters.TryRemove(pid, out var memoryPerf);
        memoryPerf?.Close();
        memoryPerf?.Dispose();

        lock (_lock)
        {
            var pidExists = ContainsId(pid);
            if (!pidExists) return;

            RemoveProcess(pid);
        }

        SendTerminatedProcessUpdate(pid);
    }

    private static Process? GetProcessIfPidExists(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            process.Refresh();
            return process.Id == 0 ? null : process;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override void Dispose(bool disposing)
    {
        ReleaseWatcher(true);
        base.Dispose(disposing);
    }

    protected virtual void ReleaseWatcher(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            if (_watcher == null) return;

            _watcher.EventArrived -= WmiEventHandler;
            _watcher.Stop();
            _watcher.Dispose();
        }
        _disposed = true;
    }
}
