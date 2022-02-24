/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace LocalCollector.Processes
{
    public abstract class ProcessInfoGeneratorBase
    {
        public abstract int? GetParentId(Process process);
        public abstract float GetMemoryUsage(Process process);
        public abstract float GetCPUUsage(Process process);
        public abstract List<ProcessInfoDto> GetChildProcesses(Process process, ProcessInfoManager manager);
        public abstract ProcessStartInfo KillProcessByName(string processName);
        public abstract ProcessStartInfo KillProcessById(int processId);
    }

    public class ProcessInfoManager
    {
        public ProcessInfoGeneratorBase ProcessInfoGenerator;

        public ProcessInfoManager(bool isLinux)
        {
            if (isLinux)
                ProcessInfoGenerator = new ProcessInfoLinux();
            else
                ProcessInfoGenerator = new ProcessInfoWindows();
        }

        public int? GetParentId(Process process)
            => ProcessInfoGenerator.GetParentId(process);
        public float GetMemoryUsage(Process process)
            => ProcessInfoGenerator.GetMemoryUsage(process);
        public float GetCPUUsage(Process process)
            => ProcessInfoGenerator.GetCPUUsage(process);
        public List<ProcessInfoDto> GetChildProcesses(Process process)
            => ProcessInfoGenerator.GetChildProcesses(process, this);
        public ProcessStartInfo KillProcessByName(string processName)
            => ProcessInfoGenerator.KillProcessByName(processName);
        public ProcessStartInfo KillProcessById(int processId)
            => ProcessInfoGenerator.KillProcessById(processId);
    }

    public class ProcessInfoLinux : ProcessInfoGeneratorBase
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

        public override float GetCPUUsage(Process process)
        {
            var result = RunLinuxPSCommand(process, "cpu");
            return float.Parse(result.Split("\n")[1], CultureInfo.InvariantCulture.NumberFormat);
        }

        public override float GetMemoryUsage(Process process)
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

        public override int? GetParentId(Process child)
        {
            string[] parts = GetLinuxInfo(child.Id);
            if (parts.Length >= 3 && parts != default) return Int32.Parse(parts[2]);
            return default;
        }

        public override List<ProcessInfoDto> GetChildProcesses(Process parent, ProcessInfoManager manager)
        {
            List<ProcessInfoDto> children = new List<ProcessInfoDto>();

            Process[] processes = Process.GetProcesses(Environment.MachineName);
            foreach (Process process in processes)
            {
                int? ppid = GetParentId(process);
                if (ppid == parent.Id && ppid != default)
                {
                    children.Add(new ProcessInfo(process, manager).Data);
                }
            }
            return children;
        }

        public override ProcessStartInfo KillProcessByName(string processName)
            => new ProcessStartInfo("/bin/bash", string.Format( " -c 'sudo pkill -f {0}'", processName));

        public override ProcessStartInfo KillProcessById(int processId)
            => new ProcessStartInfo("/bin/bash", string.Format(" -c 'sudo pkill -f {0}'", processId.ToString()));
    }

    public class ProcessInfoWindows : ProcessInfoGeneratorBase
    {
        internal ProcessInfoWindows()
        {

        }
        public override int? GetParentId(Process process)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + process.Id.ToString() + "'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return parentPid;
        }

        public override float GetMemoryUsage(Process process)
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

        public override float GetCPUUsage(Process process)
        {
            var cpu = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            return cpu.NextValue() * 100;
        }

        public override List<ProcessInfoDto> GetChildProcesses(Process process, ProcessInfoManager manager)
        {
            List<ProcessInfoDto> children = new List<ProcessInfoDto>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));
            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(new ProcessInfo(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])), manager).Data);
            }

            return children;
        }
        public static void WatchProcesses()
        {
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
                Watcher.EventArrived += new EventArrivedEventHandler(ProcessInfoWindows.WmiEventHandler);
                Watcher.Start();
                //Watcher.Stop();
            }
            catch (Exception)
            {
                throw new ApplicationException("Error while reading the processes.");
            }
        }

        private static void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            //in this point the new events arrives
            //you can access to any property of the Win32_Process class
            Console.WriteLine("TargetInstance.Handle :    " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["Handle"]);
            Console.WriteLine("TargetInstance.Name :      " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["Name"]);
            Console.WriteLine("TargetInstance.ID :      " + ((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value)["ProcessId"]);

        }

        public override ProcessStartInfo KillProcessByName(string processName)
            => new ProcessStartInfo("cmd.exe", string.Format("/c taskkill /f /im {0}", processName));

        public override ProcessStartInfo KillProcessById(int processId)
             => new ProcessStartInfo("cmd.exe", string.Format("/c taskkill /f /im {0}", processId.ToString()));
    }
}
