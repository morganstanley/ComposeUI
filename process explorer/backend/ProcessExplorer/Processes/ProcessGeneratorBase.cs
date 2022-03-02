/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Diagnostics;

namespace LocalCollector.Processes
{
    public abstract class ProcessGeneratorBase : IProcessGenerator
    {
        public Action<ProcessInfo>? SendNewProcess { get; set; }
        public Action<int>? SendTerminatedProcess { get; set; }
        public Action<int>? SendModifiedProcess { get; set; }

        internal SynchronizedCollection<int> Processes = new SynchronizedCollection<int>();

        /// <summary>
        /// Returns the PPID of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract int? GetParentId(Process process);

        /// <summary>
        /// Returns the memory usage (%) of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract float GetMemoryUsage(Process process);

        /// <summary>
        /// Returns the CPU usage (%) of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract float GetCPUUsage(Process process);

        /// <summary>
        /// Returns a list, which will contain the child processes of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract SynchronizedCollection<ProcessInfoDto> GetChildProcesses(Process process);

        /// <summary>
        /// Kills a process by the given process name.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public abstract ProcessStartInfo KillProcessByName(string processName);

        /// <summary>
        /// Kills a process by the given process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public abstract ProcessStartInfo KillProcessById(int processId);

        /// <summary>
        /// Creates an event behavior when a related process has been created.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract ProcessInfo ProcessCreated(Process process);

        /// <summary>
        /// Continuously watching created processes.
        /// </summary>
        /// <param name="processes"></param>
        public abstract void WatchProcesses(SynchronizedCollection<ProcessInfoDto> processes);

        /// <summary>
        /// Creates a list, containing the process ids, which are running under the main process.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public SynchronizedCollection<int>? GetProcessIds(SynchronizedCollection<ProcessInfoDto> processes)
        {
            SynchronizedCollection<int>? list = new SynchronizedCollection<int>();
            lock (processes)
            {
                foreach (var process in processes)
                {
                    if (process.PID != default)
                        list.Add(Convert.ToInt32(process.PID));
                    if (process.Children != default)
                    {
                        lock (process.Children)
                        {
                            foreach (var child in process.Children)
                            {
                                if (child.PID != default && child.PID != default)
                                    list.Add(Convert.ToInt32(child.PID));
                            }
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Checks if the process ID exists in the current context.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="processes"></param>
        /// <returns></returns>
        internal bool CheckIfPIDExists(int pid, SynchronizedCollection<int> processes)
        {
            lock (processes)
            {
                if (processes.Contains(pid))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the given process is related to the main process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public bool IsComposeProcess(object process, SynchronizedCollection<int> processes)
        {
            try
            {
                var proc = process as Process;
                if (proc != null && ProcessMonitor.ComposePID != default)
                    while (proc?.Id != default)
                    {
                        proc?.Refresh();
                        if (proc?.Id == ProcessMonitor.ComposePID)
                            return true;

                        int? ppid = GetParentId(proc);
                        if (ppid.HasValue) process = Process.GetProcessById(Convert.ToInt32(ppid));
                    }
            }
            catch
            {
                var pid = Convert.ToInt32(process);
                if (CheckIfPIDExists(pid, processes))
                    return true;
            }
            return false;
        }
    }
}
