/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;

namespace ProcessExplorer.Processes
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProcessInfoGeneratorWindows : ProcessInfoManager
    {
        private readonly ILogger<ProcessInfoGeneratorWindows>? logger;
        private ConcurrentDictionary<int, PerformanceCounter> cpuPerformanceCounters = new ConcurrentDictionary<int, PerformanceCounter>();
        private ConcurrentDictionary<int, PerformanceCounter> memoryPerformanceCounters = new ConcurrentDictionary<int, PerformanceCounter>();
        public ProcessInfoGeneratorWindows(ILogger<ProcessInfoGeneratorWindows>? logger)
        {
            this.logger = logger;
        }

        public ProcessInfoGeneratorWindows(EventHandler<ProcessInfo> SendNewProcess,
            EventHandler<int> SendTerminatedProcess, EventHandler<int> SendModifiedProcess,
            ILogger<ProcessInfoGeneratorWindows>? logger = null)
            : this(logger)
        {
            this.SendNewProcess += SendNewProcess;
            this.SendTerminatedProcess += SendTerminatedProcess;
            this.SendModifiedProcess += SendModifiedProcess;
        }

        internal override int? GetParentId(Process? process)
        {
            if (process is not null)
            {
                int ppid = 0;
                using (ManagementObject mo =
                       new ManagementObject(string.Format("win32_process.handle='{0}'", process.Id.ToString())))
                {
                    try
                    {
                        mo.Get();
                        ppid = Convert.ToInt32(mo["ParentProcessId"]);
                    }
                    catch (Exception exception)
                    {
                        if (process.Id > 0)
                        {
                            logger?.ManagementObjectPPID(process.Id, exception);
                        }
                    }
                }

                return ppid;
            }

            return default;
        }

        internal override float GetMemoryUsage(Process process)
        {
            int memsize;
            
            if(!memoryPerformanceCounters.TryGetValue(process.Id, out var perf))
            {
                perf = new PerformanceCounter();
                perf.CategoryName = "Process";
                perf.CounterName = "Working Set - Private";
                perf.InstanceName = process.ProcessName;
                memoryPerformanceCounters[process.Id] = perf;
            }
            memsize = Convert.ToInt32(perf.NextValue()) / Convert.ToInt32(1024) / Convert.ToInt32(1024);
            return (float)((memsize / GetTotalMemoryInMB()) * 100);
        }

        private static double GetTotalMemoryInMB()
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            return Convert.ToDouble(installedMemory) / 1048576.0;
        }

        internal override float GetCPUUsage(Process process)
        {
            if(!cpuPerformanceCounters.TryGetValue(process.Id, out var performanceCounter))
            {
                performanceCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                cpuPerformanceCounters[process.Id] = performanceCounter;
            }
            
            float processCpuUsage;
            processCpuUsage = performanceCounter.NextValue();

            return processCpuUsage / Environment.ProcessorCount;
        }

        internal override SynchronizedCollection<ProcessInfoData> GetChildProcesses(Process process)
        {
            SynchronizedCollection<ProcessInfoData> children = new SynchronizedCollection<ProcessInfoData>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(
                string.Format("Select * From Win32_Process Where ParentProcessID={0} Or ProcessID={0}", process.Id));

            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject)o;
                try
                {
                    var proc = new ProcessInfo(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])), this);
                    if (proc.Data != default)
                    {
                        children.Add(proc.Data);
                    }
                }
                catch (Exception exception)
                {
                    logger?.ManagementObjectChildError(exception);
                }
            }

            return children;
        }

        internal override void WatchProcesses(SynchronizedCollection<ProcessInfoData> processes)
        {
            this.ProcessIds = GetProcessIds(processes);
            try
            {
                string WmiQuery;
                ManagementEventWatcher Watcher;
                ManagementScope Scope;

                Scope = new ManagementScope(@"\\.\root\CIMV2");
                Scope.Connect();

                WmiQuery = "Select * From __InstanceOperationEvent Within 1 " +
                           "Where TargetInstance ISA 'Win32_Process' ";

                Watcher = new ManagementEventWatcher(Scope, new EventQuery(WmiQuery));
                Watcher.EventArrived += WmiEventHandler;
                Watcher.Start();
            }
            catch (Exception exception)
            {
                logger?.ManagementObjectWatchError(exception);
            }
        }

        internal override void AddChildProcessesToList()
        {
            try
            {
                Process main = Process.GetProcessById(ProcessMonitor.ComposePID);

                var children = GetChildProcesses(main);

                foreach (var child in children)
                {
                    if (child is not null && child.PID is not null && child.ParentId is not null)
                    {
                        var ppid = Convert.ToInt32(child.ParentId);
                        var pid = Convert.ToInt32(child.PID);
                        SendNewDataIfPPIDExists(ppid, pid);
                    }
                }
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcessError(exception);
            }
        }

        private void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            int pid = Convert.ToInt32(
                ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

            string? wclass = (e.NewEvent).SystemProperties["__Class"].Value.ToString();
            if (wclass is not null)
            {
                try
                {
                    switch (wclass)
                    {
                        case "__InstanceCreationEvent":
                            InstanceCreated(pid);
                            break;

                        case "__InstanceDeletionEvent":
                            InstanceDeletion(pid);
                            break;

                        case "__InstanceModificationEvent":
                            SendModifiedIfData(pid);
                            break;
                    }
                }
                catch (Exception exception)
                {
                    logger?.ManagementObjectWatchEventError(exception);
                }
            }
        }

        private void InstanceCreated(int pid)
        {
            var process = ReturnProcessIfExists(pid);
            if (process != default)
            {
                int ppid = Convert.ToInt32(GetParentId(process));
                SendNewDataIfPPIDExists(ppid, pid);
            }
        }

        private void InstanceDeletion(int pid)
        {
            cpuPerformanceCounters.TryRemove(pid, out var cpuPerf);
            cpuPerf?.Close();
            cpuPerf?.Dispose();
            memoryPerformanceCounters.TryRemove(pid, out var memoryPerf);
            memoryPerf?.Close();
            memoryPerf?.Dispose();
            SendDeletedDataPIDToCheckAsync(pid);
        }

        protected Process? ReturnProcessIfExists(int pid)
        {
            var process = Process.GetProcessById(pid);
            process.Refresh();

            if (process.Id != 0)
            {
                return process;
            }

            return default;
        }

        internal override Process? KillProcessByName(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).FirstOrDefault();
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcessError(exception);
            }

            return null;
        }

        internal override Process? KillProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcessError(exception);
            }

            return null;
        }
    }
}
