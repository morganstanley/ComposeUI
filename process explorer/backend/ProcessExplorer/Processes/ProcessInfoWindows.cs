/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes.Logging;
using System.Diagnostics;
using System.Management;

namespace ProcessExplorer.Processes
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProcessInfoWindows : ProcessGeneratorBase
    {
        private readonly ILogger<ProcessInfoWindows>? logger;
        private readonly object locker = new object();
        public ProcessInfoWindows(ILogger<ProcessInfoWindows>? logger)
        {
            this.logger = logger;
        }

        public ProcessInfoWindows(EventHandler<ProcessInfo> SendNewProcess, EventHandler<int> SendTerminatedProcess, EventHandler<int> SendModifiedProcess, ILogger<ProcessInfoWindows>? logger = null)
            : this(logger)
        {
            this.SendNewProcess += SendNewProcess;
            this.SendTerminatedProcess += SendTerminatedProcess;
            this.SendModifiedProcess += SendModifiedProcess;
        }

        public override int? GetParentId(Process? process)
        {
            if (process is not null)
            {
                int ppid = 0;
                using (ManagementObject mo = new ManagementObject(string.Format("win32_process.handle='{0}'", process.Id.ToString())))
                {
                    try
                    {
                        mo.Get();
                        ppid = Convert.ToInt32(mo["ParentProcessId"]);
                    }
                    catch (Exception exception)
                    {
                        if (process.Id > 0)
                            logger?.ManagementObjectPPIDError(process.Id, exception);
                    }
                }
                return ppid;
            }
            return default;
        }

        public override float GetMemoryUsage(Process process)
        {
            int memsize;
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = process.ProcessName;
            memsize = Convert.ToInt32(PC.NextValue()) / Convert.ToInt32(1024) / Convert.ToInt32(1024);
            PC.Close();
            PC.Dispose();
            return (float)((memsize / GetTotalMemoryInMB()) * 100);
        }

        private static double GetTotalMemoryInMB()
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            return Convert.ToDouble(installedMemory) / 1048576.0;
        }

        public override float GetCPUUsage(Process process)
        {
            var cpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
            cpu.NextValue();
            return cpu.NextValue() / Environment.ProcessorCount;
        }

        public override SynchronizedCollection<ProcessInfoData> GetChildProcesses(Process process)
        {
            SynchronizedCollection<ProcessInfoData> children = new SynchronizedCollection<ProcessInfoData>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ParentProcessID={0} Or ProcessID={0}", process.Id));
            foreach (var o in mos.Get())
            {
                var mo = (ManagementObject) o;
                try
                {
                    var proc = new ProcessInfo(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])), this);
                    if (proc.Data != default)
                        lock (locker)
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

        public override void WatchProcesses(SynchronizedCollection<ProcessInfoData> processes)
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

        public override void AddChildProcessesToList()
        {
            try
            {
                Process main = Process.GetProcessById(ProcessMonitor.ComposePID);

                var children = GetChildProcesses(main);
                lock (locker)
                {
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
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcess(exception);
            }
        }

        private void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            int pid = Convert.ToInt32(((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

            string? wclass = (e.NewEvent).SystemProperties["__Class"].Value.ToString();
            if (wclass is not null)
            {
                try
                {
                    Process? process;
                    switch (wclass)
                    {
                        case "__InstanceCreationEvent":
                            process = ReturnProcessIfExists(pid);
                            if (process != default)
                            {
                                int ppid = Convert.ToInt32(GetParentId(process));
                                SendNewDataIfPPIDExists(ppid, pid);
                            }
                            break;

                        case "__InstanceDeletionEvent":
                            SendDeletedDataPIDToCheckAsync(pid);
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

        protected Process? ReturnProcessIfExists(int pid)
        {
            var process = Process.GetProcessById(pid);
            process.Refresh();

            if (process.Id != 0)
            {
                lock (locker)
                    return process;
            }
            return default;
        }

        public override Process? KillProcessByName(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).FirstOrDefault();
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcess(exception);
            }

            return null;
        }

        public override Process? KillProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (Exception exception)
            {
                logger?.CannotFindProcess(exception);
            }

            return null;
        }

    }
}
