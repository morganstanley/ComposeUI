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
using ProcessExplorer.Abstractions.Logging;
using ProcessExplorer.Abstractions.Processes;

namespace ProcessExplorer.Core.Processes;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class WindowsProcessInfoMonitor : ProcessInfoMonitor
{
    private readonly ILogger<ProcessInfoMonitor> _logger;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _cpuPerformanceCounters;
    private readonly ConcurrentDictionary<int, PerformanceCounter> _memoryPerformanceCounters;
    private readonly ConcurrentDictionary<int, ManagementObjectSearcher> _managementPidObjectSearchers;
    private readonly ConcurrentDictionary<int, ManagementObjectSearcher> _managementPPidObjectSearchers;

    private ManagementEventWatcher? _watcher;
    private bool _disposed = false;
    private readonly object _lock = new();
    public WindowsProcessInfoMonitor(ILogger<ProcessInfoMonitor>? logger)
        :base(logger)
    {
        _logger = logger ?? NullLogger<WindowsProcessInfoMonitor>.Instance;
        _cpuPerformanceCounters = new();
        _memoryPerformanceCounters = new();
        _managementPidObjectSearchers = new();
        _managementPPidObjectSearchers = new();
    }

    public override void StopWatchingProcesses()
    {
        if (_watcher == null) return;
        _watcher.Stop();
    }

    public override int? GetParentId(int processId, string processName)
    {
        var parentProcessId = 0;

        try
        {
            if (Process.GetProcessById(processId) == null) return null;

            var managementObjectSearcher = _managementPPidObjectSearchers.GetOrAdd(processId, _ => new ManagementObjectSearcher(
                $"Select ParentProcessId From Win32_Process Where ProcessID={processId}"));


            using var mo = managementObjectSearcher.Get();
            var deviceArray = new ManagementBaseObject[mo.Count];

            mo.CopyTo(deviceArray, 0);

            if (deviceArray.Length <= 0) return null;

            parentProcessId = Convert.ToInt32(deviceArray.First()["ParentProcessId"]);
        }
        catch (Exception exception)
        {
            _logger.ManagementObjectPpidExpected(processId, exception);
            return null;
        }

        return parentProcessId;
    }

    public override float GetMemoryUsage(int processId, string? processName)
    {
        int memsize;

        if (processName == null) return default;

        using var memoryPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(processId, _ => new PerformanceCounter()
        {
            CategoryName = "Process",
            CounterName = "Working Set - Private",
            InstanceName = processName,
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

    public override float GetCpuUsage(int processId, string? processName)
    {
        if (processName == null) return default;

        using var cpuPerformanceCounter = _memoryPerformanceCounters.GetOrAdd(
            processId, _ =>
            {
                return new PerformanceCounter(
                    "Process",
                    "% Processor Time",
                    processName,
                    true);
            });
            

        cpuPerformanceCounter.NextValue();

        var processCpuUsage = cpuPerformanceCounter.NextValue();

        return processCpuUsage / Environment.ProcessorCount;
    }

    private ReadOnlySpan<int> GetChildProcesses(int processId, string processName)
    {
        if (processName == null) return default;

        using var managementObjectSearcher = _managementPidObjectSearchers.GetOrAdd(processId, _ => new ManagementObjectSearcher(
            string.Format("Select ProcessId From Win32_Process Where ParentProcessID={0} Or ProcessID={0}", processId)));

        var managementCollection = managementObjectSearcher.Get();
        var children = new int[managementCollection.Count];

        var i = 0;

        foreach (var managementBaseObject in managementCollection)
        {
            using var managementObject = (ManagementObject)managementBaseObject;

            try
            {
                var childProcessId = Convert.ToInt32(managementObject["ProcessId"]);
                children[i] = childProcessId;
                i++;

                managementCollection = managementObjectSearcher.Get();
            }
            catch (Exception exception)
            {
                _logger.ChildProcessExpected(exception);
            }
        }

        return children;
    }

    public override void WatchProcesses(int mainProcessId)
    {
        _logger.ProcessMonitorEnabledDebug();

        base.WatchProcesses(mainProcessId);

        const string wmiQuery =
"SELECT TargetInstance.ProcessId " +
"FROM __InstanceOperationEvent WITHIN 3 " +
"WHERE TargetInstance ISA 'Win32_Process'";

        try
        {
            if(_watcher == null)
            {
                var scope = new ManagementScope(@"\\.\root\CIMV2");
                scope.Connect();

                _watcher = new ManagementEventWatcher(scope, new EventQuery(wmiQuery));
                _watcher.EventArrived += WmiEventHandler;
            }

            _watcher.Start();
        }
        catch (Exception exception)
        {
            _logger.WatcherInitializationError(exception);
        }
    }

    public override ReadOnlySpan<int> AddChildProcesses(int processId, string? processName)
    {
        lock (_lock)
        {
            if (!ContainsId(processId))
            {
                AddProcess(processId);
            }
        }

        var processes = new List<int>() { processId };

        try
        {
            var childrenProcessIds = GetChildProcesses(processId, processName ?? string.Empty);

            foreach (var childProcessId in childrenProcessIds)
            {
                if (childProcessId == processId) continue;

                processes.Add(childProcessId);

                AddIfComposeProcess(childProcessId);

                var childrenOfChildIds = AddChildProcesses(
                        childProcessId, 
                        GetProcessIfProcessIdExists(childProcessId)?.ProcessName ?? null);

                foreach (var childOfChildId in childrenOfChildIds)
                {
                    if (processes.Contains(childOfChildId)) continue;

                    processes.Add(childOfChildId);
                    AddIfComposeProcess(childOfChildId);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.PpidExpected(processId, exception);
        }

        return processes.ToArray();
    }

    private void AddIfComposeProcess(int processId)
    {
        lock (_lock)
        {
            if (!CheckIfIsComposeProcess(processId)) return;

            var alreadyAdded = ContainsId(processId);
            if (alreadyAdded) return;

            AddProcess(processId);
        }
    }

    private void WmiEventHandler(object sender, EventArrivedEventArgs e)
    {
        var processId = Convert.ToInt32(
            ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

        var eventDefinition = e.NewEvent.SystemProperties["__Class"]
            .Value
            .ToString();

        if (eventDefinition == null) return;

        try
        {
            switch (eventDefinition)
            {
                case "__InstanceCreationEvent":
                    InstanceCreated(processId);
                    break;

                case "__InstanceDeletionEvent":
                    InstanceDeleted(processId);
                    break;

                case "__InstanceModificationEvent":
                    InstanceModified(processId);
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.InstanceEventExpected(exception);
        }
    }

    private void InstanceModified(int processId)
    {
        lock (_lock)
        {
            if (ContainsId(processId))
            {
                ProcessModifiedUpdate(processId);
            }
        }
    }

    private void InstanceCreated(int processId)
    {
        var process = GetProcessIfProcessIdExists(processId);

        if (process == null) return;

        lock (_lock)
        {
            if (!CheckIfIsComposeProcess(processId)) return;

            AddProcess(processId);
        }
    }

    private void InstanceDeleted(int processId)
    {
        var _cpuPerformanceCounterRemoved = _cpuPerformanceCounters.TryRemove(processId, out var cpuPerf);
        if (_cpuPerformanceCounterRemoved && cpuPerf != null)
        {
            cpuPerf.Close();
            cpuPerf.Dispose();
        }

        var memoryPerformanceCounterRemoved = _memoryPerformanceCounters.TryRemove(processId, out var memoryPerf);
        if (memoryPerformanceCounterRemoved && memoryPerf != null)
        {
            memoryPerf.Close();
            memoryPerf.Dispose();
        }

        var managementPidSearcherRemoved = _managementPidObjectSearchers.TryRemove(processId, out var objectSearcher);
        if (managementPidSearcherRemoved && objectSearcher != null) objectSearcher.Dispose();

        lock (_lock)
        {
            RemoveProcessId(processId);
        }
    }

    private static Process? GetProcessIfProcessIdExists(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
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
