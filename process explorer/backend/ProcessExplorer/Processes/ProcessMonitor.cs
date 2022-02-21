/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;

namespace ProcessExplorer.Entities
{
    public class ProcessMonitor
    {
        public ConcurrentBag<ProcessInfo> processes;
        public ProcessMonitor()
        {
            processes = new ConcurrentBag<ProcessInfo>();
            ClearBag();
            FillBag();
        }

        public ProcessMonitor(ConcurrentBag<ProcessInfo> processes)
            => this.processes = processes;

        public ProcessMonitor(bool windows)
            :this()
        {
            if (windows)
            {
                ReadProcessesFromSource();
            }
        }

        private void ClearBag() => processes.Clear();
        private void FillBag()
        {
            processes.Add(new ProcessInfo(Process.GetCurrentProcess()));
        }

        public ConcurrentBag<ProcessInfo> GetBag() 
        {
            ClearBag();
            FillBag();
            return processes;
        }

        private IEnumerable<ProcessInfo> GetProcessByName(string name) => processes.Where(p => p.ProcessName == name);
        private IEnumerable<ProcessInfo> GetProcessByID(int id) => processes.Where(p => p.ProcessId == id);
        public async void KillProcess(string command, bool isLinux)
        {
            try
            {
                ProcessStartInfo info;
                if (!isLinux)
                {
                    info = new ProcessStartInfo("cmd.exe", "/c " + command);
                }
                else
                {
                    info = new ProcessStartInfo(string.Format("/bin/bash", " -c 'sudo pkill -f {0}'"), command);
                }
                info.RedirectStandardError = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                Process process = new Process();
                process.StartInfo = info;
                process.Start();
                process.StandardOutput.ReadToEnd();
                await process.WaitForExitAsync();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public void KillProcessByName(string processName, bool isLinux)
        {
            string taskKiller;
            if (isLinux)
                taskKiller = processName;
            else
                taskKiller = "taskkill /f /im " + processName;
            KillProcess(taskKiller, isLinux);
        }

        public void KillProcessById(int processId, bool isLinux)
        {
            string taskKiller;
            if (isLinux)
                taskKiller = processId.ToString();
            else
                taskKiller = "taskkill /f /im " + processId;
            KillProcess(taskKiller, isLinux);
        }

        public void ReadProcessesFromSource()
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
                Watcher.EventArrived += new EventArrivedEventHandler(this.WmiEventHandler);
                Watcher.Start();
                Watcher.Stop();
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

        }
    }
}
