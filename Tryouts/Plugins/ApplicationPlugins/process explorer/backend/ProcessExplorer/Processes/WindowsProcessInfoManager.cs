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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Core.Logging;

namespace ProcessExplorer.Core.Processes;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class WindowsProcessInfoManager : ProcessInfoManager
{
    private readonly ILogger<ProcessInfoManager> _logger;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _cpuPerformanceCounters;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _memoryPerformanceCounters;
    private readonly ConcurrentDictionary<int, ManagementObjectSearcher> _managementPidObjectSearchers;
    private readonly ConcurrentDictionary<int, ManagementObjectSearcher> _managementPPidObjectSearchers;

    private ManagementEventWatcher? _watcher;
    private bool _disposed = false;
    private readonly object _lock = new();
    public WindowsProcessInfoManager(ILogger<ProcessInfoManager>? logger)
    {
        _logger = logger ?? NullLogger<WindowsProcessInfoManager>.Instance;
        _cpuPerformanceCounters = new();
        _memoryPerformanceCounters = new();
        _managementPidObjectSearchers = new();
        _managementPPidObjectSearchers = new();
    }

    public override int? GetParentId(int processId, string processName)
    {
        var ppid = 0;

        try
        {
            //snapshot
            if (!Process.GetProcesses().Any(p => p.Id == processId)) return null;

            var managementObjectSearcher = _managementPPidObjectSearchers.GetOrAdd(processId, _ =>
            {
                return new ManagementObjectSearcher(
                    string.Format(
                        "Select ParentProcessId From Win32_Process Where ParentProcessID={0} Or ProcessID={0}",
                        processId));
            });


            using var mo = managementObjectSearcher.Get();
            var deviceArray = new ManagementObject[mo.Count];

            mo.CopyTo(deviceArray, 0);

            if (deviceArray.Length <= 0) return null;

            ppid = Convert.ToInt32(deviceArray.First()["ParentProcessId"]); //managementObjectSearcher["ParentProcessId"]
        }
        catch (Exception exception)
        {
            _logger.ManagementObjectPpidExpected(processId, exception);
        }

        return ppid;
    }

    public override float GetMemoryUsage(int id, string? processName)
    {
        int memsize;

        if (processName == null) return default;

        using var memoryPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(id, _ =>
        {
            return new PerformanceCounter()
            {
                CategoryName = "Process",
                CounterName = "Working Set - Private",
                InstanceName = processName,
            };
        });

        memsize = Convert.ToInt32(memoryPerformanceCounter.NextValue()) / Convert.ToInt32(1024) / Convert.ToInt32(1024);

        return (float)(memsize / GetTotalMemoryInMb() * 100);
    }

    private static double GetTotalMemoryInMb()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
        return Convert.ToDouble(installedMemory) / 1048576.0;
    }

    public override float GetCpuUsage(int id, string? processName)
    {
        if (processName == null) return default;
        using var cpuPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(
            id,
            _ =>
            {
                return new PerformanceCounter(
                            "Process",
                            "% Processor Time",
                            processName,
                            true);
            });

        var processCpuUsage = cpuPerformanceCounter.NextValue();

        return processCpuUsage / Environment.ProcessorCount;
    }

    public ReadOnlySpan<int> GetChildProcesses(int id, string processName)
    {
        if (processName == null) return default;

        using var managementObjectSearcher = _managementPidObjectSearchers.GetOrAdd(id, _ =>
        {
            return new ManagementObjectSearcher(
                string.Format(
                    "Select ProcessId From Win32_Process Where ParentProcessID={0} Or ProcessID={0}",
                    id));
        });

        var mo = managementObjectSearcher.Get();
        var children = new int[mo.Count];

        var i = 0;

        foreach (var managementObjectCollection in mo)
        {
            using var managementObject = (ManagementObject)managementObjectCollection;

            try
            {
                var pid = Convert.ToInt32(managementObject["ProcessId"]);
                children[i] = pid;
                i++;

                mo = managementObjectSearcher.Get();
            }
            catch (Exception exception)
            {
                _logger.ChildProcessExpected(exception);
            }
        }

        return children;
    }

    public override void WatchProcesses()
    {
        const string wmiQuery =
"SELECT TargetInstance.ProcessId " +
"FROM __InstanceOperationEvent WITHIN 3 " +
"WHERE TargetInstance ISA 'Win32_Process'"; // And __Class = '__InstanceModificationEvent'

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

    public override ReadOnlySpan<int> AddChildProcesses(int pid, string? processName)
    {
        lock (_lock)
        {
            if (!ContainsId(pid))
            {
                AddProcess(pid);
            }
        }

        var processes = new List<int>() { pid };

        try
        {
            var children = GetChildProcesses(pid, processName ?? string.Empty);

            foreach (var child in children)
            {
                if (child == pid) continue;

                processes.Add(child);

                AddIfComposeProcess(child);

                var childrenOfChild = AddChildProcesses(child, Process.GetProcessById(child).ProcessName);
                foreach (var cChild in childrenOfChild)
                {
                    if (processes.Contains(cChild)) continue;

                    processes.Add(cChild);
                    AddIfComposeProcess(cChild);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.PpidExpected(pid, exception);
        }

        return processes.ToArray();
    }

    private void AddIfComposeProcess(int pid)
    {
        lock (_lock)
        {
            if (!CheckIfIsComposeProcess(pid)) return;

            var alreadyAdded = ContainsId(pid);
            if (alreadyAdded) return;

            AddProcess(pid);
        }
    }

    private void WmiEventHandler(object sender, EventArrivedEventArgs e)
    {
        var pid = Convert.ToInt32(
            ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

        var wclass = e.NewEvent.SystemProperties["__Class"]
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
            if (ContainsId(pid))
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
    }

    private void InstanceDeleted(int pid)
    {
        _cpuPerformanceCounters.TryRemove(pid, out var cpuPerf);
        cpuPerf?.Close();
        cpuPerf?.Dispose();

        _memoryPerformanceCounters.TryRemove(pid, out var memoryPerf);
        memoryPerf?.Close();
        memoryPerf?.Dispose();

        _managementPidObjectSearchers.TryRemove(pid, out var objectSearcher);
        objectSearcher?.Dispose();

        lock (_lock)
        {
            var pidExists = ContainsId(pid);
            if (!pidExists) return;

            RemoveProcessId(pid);
        }

        //SendTerminatedProcessUpdate(pid);
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
