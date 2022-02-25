/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace LocalCollector.Processes
{
    public interface IProcessGenerator
    {
        public int? GetParentId(Process process);
        public float GetMemoryUsage(Process process);
        public float GetCPUUsage(Process process);
        public List<ProcessInfoDto> GetChildProcesses(Process process);
        public ProcessStartInfo KillProcessByName(string processName);
        public ProcessStartInfo KillProcessById(int processId);
        public void ProcessChanged(Process process);
        public void WatchProcesses(List<int> processes);

        public List<int> GetProcessIds(List<ProcessInfoDto> processes)
        {
            var list = new List<int>();
            foreach (var process in processes)
            {
                if (process.PID != default)
                    list.Add((int)process.PID);
                if (process.Children != default)
                {
                    foreach (var child in process.Children)
                    {
                        if (child.PID != default)
                            list.Add((int)child.PID);
                    }
                }
            }
            return list;
        }
    }

    public class ProcessInfoLinux : IProcessGenerator
    {
        internal ProcessInfoLinux()
        {

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

        public float GetCPUUsage(Process process)
        {
            var result = RunLinuxPSCommand(process, "cpu");
            return float.Parse(result.Split("\n")[1], CultureInfo.InvariantCulture.NumberFormat);
        }

        public float GetMemoryUsage(Process process)
        {
            var result = RunLinuxPSCommand(process, "mem");
            return float.Parse(result.Split("\n")[1], CultureInfo.InvariantCulture.NumberFormat);
        }

        public string[] GetLinuxInfo(int id)
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

        public int? GetParentId(Process child)
        {
            string[] parts = GetLinuxInfo(child.Id);
            if (parts.Length >= 3 && parts != default) return Int32.Parse(parts[2]);
            return default;
        }

        public List<ProcessInfoDto> GetChildProcesses(Process parent)
        {
            List<ProcessInfoDto> children = new List<ProcessInfoDto>();

            Process[] processes = Process.GetProcesses(Environment.MachineName);
            foreach (Process process in processes)
            {
                int? ppid = GetParentId(process);
                if (ppid == parent.Id && ppid != default)
                {
                    children.Add(new ProcessInfo(process, this).Data);
                }
            }
            return children;
        }

        public ProcessStartInfo KillProcessByName(string processName)
            => new ProcessStartInfo("/bin/bash", string.Format( " -c 'sudo pkill -f {0}'", processName));

        public ProcessStartInfo KillProcessById(int processId)
            => new ProcessStartInfo("/bin/bash", string.Format(" -c 'sudo pkill -f {0}'", processId.ToString()));


        public void ProcessChanged(Process process)
        {
            throw new NotImplementedException();
        }

        public void WatchProcesses(List<int> processes)
        {
            throw new NotImplementedException();
        }
    }

    public class ProcessInfoWindows : IProcessGenerator
    {
        private List<int> processes;
        internal ProcessInfoWindows()
        {

        }
        public int? GetParentId(Process process)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + process.Id.ToString() + "'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return parentPid;
        }

        public float GetMemoryUsage(Process process)
        {
            int memsize = 0;
            PerformanceCounter PC = new PerformanceCounter();
            PC.CategoryName = "Process";
            PC.CounterName = "Working Set - Private";
            PC.InstanceName = process.ProcessName;
            memsize = Convert.ToInt32(PC.NextValue()) / (int)(1024) / (int)1024;
            PC.Close();
            PC.Dispose();
            return (float)(memsize / GetTotalMemoryInMB()) * 100;
        }

        private static double GetTotalMemoryInMB()
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var installedMemory = gcMemoryInfo.TotalAvailableMemoryBytes;
            return (double)installedMemory / 1048576.0;
        }

        public float GetCPUUsage(Process process)
        {
            var cpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            return cpu.NextValue() * 100;
        }

        public List<ProcessInfoDto> GetChildProcesses(Process process)
        {
            List<ProcessInfoDto> children = new List<ProcessInfoDto>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));
            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(new ProcessInfo(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])), this).Data);
            }

            return children;
        }
        public void WatchProcesses(List<int> processes)
        {
            this.processes = processes;
            // create the watcher and start to listen
            try
            {
                string WmiQuery;
                ManagementEventWatcher Watcher;
                ManagementScope Scope;

                Scope = new ManagementScope(@"\\.\root\CIMV2");
                Scope.Connect();

                WmiQuery = "Select * From __InstanceCreationEvent Within 1 " +
                "Where TargetInstance ISA 'Win32_Process' ";

                Watcher = new ManagementEventWatcher(Scope, new EventQuery(WmiQuery));
                Watcher.EventArrived += new EventArrivedEventHandler(WmiEventHandler);
                Watcher.Start();
                //Watcher.Stop();
            }
            catch (Exception)
            {
                throw new ApplicationException("Error while reading the processes.");
            }
        }

        private void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            //in this point the new events arrives
            //you can access to any property of the Win32_Process class
            Console.WriteLine("TargetInstance.Handle :    " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["Handle"]);
            Console.WriteLine("TargetInstance.Name :      " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["Name"]);
            Console.WriteLine("TargetInstance.ID :      " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);
            int pid = Convert.ToInt32(((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);
            if(processes.Contains(pid))
            {
                ProcessChanged(Process.GetProcessById(pid));
            }
        }

        public ProcessStartInfo KillProcessByName(string processName)
            => new ProcessStartInfo("cmd.exe", string.Format("/c taskkill /f /im {0}", processName));

        public ProcessStartInfo KillProcessById(int processId)
             => new ProcessStartInfo("cmd.exe", string.Format("/c taskkill /f /im {0}", processId.ToString()));

        public void ProcessChanged(Process process)
        {
            throw new NotImplementedException();
        }
    }
}
