/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;

namespace ProcessExplorer.Entities
{
    public enum ProcessStatus
    {
        Running = 1,
        Stopped = 0
    }

    public class ProcessInfo
    {
        public ProcessInfo(Process process)
        {
            //should refresh the process, because it can be already terminated
            process.Refresh();
            //process.HasExited and .StartTime has a bug due to the not having enough rights or they are elevated process, 
            //and not a result of a process.Start call
            if (process != null && process.Id != 0)
            {
                try
                {
                    StartTime = process.StartTime;
                    ProcessorUsage = process.TotalProcessorTime;
                    PhysicalMemoryUsageB = process.WorkingSet64;
                    Status = process.HasExited == false ? ProcessStatus.Running : ProcessStatus.Stopped;
                    UserProcessorTime = process.UserProcessorTime;
                    ProcessPriorityClass = process.PriorityClass;
                    VirtualMemorySize = process.VirtualMemorySize64;
                    foreach (ProcessThread processThread in process.Threads)
                    {
                        Threads?.Append(processThread);
                    }
                }
                catch (Exception)
                {
                    Status = ProcessStatus.Stopped;
                }
                finally
                {
                    ProcessId = process.Id;
                    ProcessName = process.ProcessName;
                    PriorityLevel = process.BasePriority;
                    PagedMemoryUsage = process.PagedMemorySize64;
                    NonPagedMemoryUsage = process.NonpagedSystemMemorySize64;
                    PagedSystemMemoryUsage = process.PagedSystemMemorySize64;
                    NonPagedSystemMemoryUsage = process.NonpagedSystemMemorySize64;
                    PrivateMemoryUsage = process.PrivateMemorySize64;
                    IsLinux = (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                    
                    if (!IsLinux)
                    {
                        Children = GetChildProcessesWindows(process);
                        ParentId = GetParentIdWindows(process);
                        MemoryUsagePercentage = GetMemoryUsageWindows(process) * 100;
                        CPUUsagePercentage = GetCPUUsageWindows(process);
                    }
                    else
                    {
                        Children = GetChildProcessesLinux(process);
                        ParentId = GetParentIdLinux(process);
                        MemoryUsagePercentage = GetMemoryUsageLinux(process);
                        CPUUsagePercentage = GetCPUUsageLinux(process);
                    }
                }
            }
        }

        private int? GetParentIdWindows(Process process)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + process.Id.ToString() + "'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return parentPid;
        }

        public ProcessInfo(int processId)
            : this(Process.GetProcessById(processId))
        {

        }

        //ps -aux
        public float GetMemoryUsageWindows(Process process)
        {
            int memsize = 0; // memsize in KB
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = process.ProcessName;
            memsize = Convert.ToInt32(PC.NextValue()) / (int)(1024) / (int) 1024;
            PC.Close();
            PC.Dispose();
            return (float)(memsize / ProcessInfo.GetTotalMemoryInMB());
        }

        private static double GetTotalMemoryInMB() 
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            return (double)installedMemory / 1048576.0;
        }

        public float GetCPUUsageWindows(Process process)
        {
            var cpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            return cpu.NextValue() * 100;
        }

        private string RunLinuxPSCommand(Process process, string command)
        {
            object locker = new object();

            string result;
            var cli = new Process()
            {
                //cpu, mem
                StartInfo = new ProcessStartInfo("/bin/ps", string.Format("-p {0} -o %{1}", process.Id, command))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            cli.Start();

            lock (locker)
            {
                result = cli.StandardOutput.ReadToEnd();
            }

            cli.Close();
            return result;
        }
        public float GetCPUUsageLinux(Process process)
        {
            var result = RunLinuxPSCommand(process, "cpu");
            return float.Parse(result.Split("\n")[1], CultureInfo.InvariantCulture.NumberFormat);
        }

        public float GetMemoryUsageLinux(Process process)
        {
            var result = RunLinuxPSCommand(process, "mem");
            return float.Parse(result.Split("\n")[1], CultureInfo.InvariantCulture.NumberFormat);
        }

        //ps -aefj
        public IEnumerable<ProcessInfo> GetChildProcessesWindows(Process process)
        {
            List<ProcessInfo> children = new List<ProcessInfo>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));
            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(new ProcessInfo(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]))));
            }

            return children;
        }

        internal string[] GetLinuxInfo(int id)
        {
            string? line;

            using (StreamReader? reader = new StreamReader("/proc/" + id + "/stat"))
            {
                line = reader.ReadLine();
            }

            if (line == null) return default;

            int endOfName = line.LastIndexOf(')');
            return line.Substring(endOfName).Split(new char[] { ' ' }, 4);
        }

        internal int GetParentIdLinux(Process child)
        {
            string[] parts = GetLinuxInfo(child.Id);
            if (parts.Length >= 3 && parts != default) return Int32.Parse(parts[2]);
            return default;
        }

        internal IEnumerable<ProcessInfo> GetChildProcessesLinux(Process parent)
        {
            List<ProcessInfo> children = new List<ProcessInfo>();

            Process[] processes = Process.GetProcesses(Environment.MachineName);
            foreach (Process process in processes)
            {
                int ppid = GetParentIdLinux(process);
                if (ppid == parent.Id)
                {
                    children.Add(new ProcessInfo(process));
                }
            }
            return children;
        }

        public DateTime? StartTime { get; internal set; } = default;
        public TimeSpan? ProcessorUsage { get; set; } = default;
        public long PhysicalMemoryUsageB { get; set; } = default;
        public string? ProcessName { get; set; } = default;
        public int ProcessId { get; set; } = default;
        public int PriorityLevel { get; set; } = default;
        public ProcessPriorityClass ProcessPriorityClass { get; set; } = default;
        public ProcessThread[]? Threads { get; private set; } = default;
        public long VirtualMemorySize { get; private set; } = default;
        public int? ParentId { get; set; } = null;
        public long PagedMemoryUsage { get; set; } = default;
        public long NonPagedMemoryUsage { get; set; } = default;
        public long PagedSystemMemoryUsage { get; set; } = default;
        public long NonPagedSystemMemoryUsage { get; set; } = default;
        public long PrivateMemoryUsage { get; set; } = default;
        public TimeSpan UserProcessorTime { get; set; } = default;
        public ProcessStatus Status { get; set; } = ProcessStatus.Running;
        public bool IsLinux { get; set; } = false;
        public IEnumerable<ProcessInfo>? Children { get; private set; } = default;
        public float MemoryUsagePercentage { get; set; } = default;
        public float CPUUsagePercentage { get; set;} = default;
    }
}
