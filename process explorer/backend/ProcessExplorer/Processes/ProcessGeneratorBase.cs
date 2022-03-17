/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities;
using ProcessExplorer.Processes;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LocalCollector.Processes
{
    public abstract class ProcessGeneratorBase : IProcessGenerator
    {
        public Action<ProcessInfo>? SendNewProcess { get; set; }
        public Action<int>? SendTerminatedProcess { get; set; }
        public Action<int>? SendModifiedProcess { get; set; }

        internal ConcurrentDictionary<int, byte[]>? ProcessIds = new ConcurrentDictionary<int, byte[]>();
        private readonly object locker = new object();

        /// <summary>
        /// Returns the PPID of the given process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public abstract int? GetParentId(Process? process);

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
        public ProcessInfo ProcessCreated(Process process)
            => new ProcessInfo(process, this);

        /// <summary>
        /// Continuously watching created processes.
        /// </summary>
        /// <param name="processes"></param>
        public abstract void WatchProcesses(SynchronizedCollection<ProcessInfoDto> processes);

        /// <summary>
        /// It will add the childs to the list which is containing the relevant processes
        /// </summary>
        public void AddChildProcessesToList()
        {
            Process main = Process.GetProcessById(ProcessMonitor.ComposePID);
            if (main is not null)
            {
                var children = GetChildProcesses(main);
                lock (locker)
                {
                    if (children is not null)
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
        }

        /// <summary>
        /// Creates a list, containing the process ids, which are running under the main process.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public ConcurrentDictionary<int, byte[]> GetProcessIds(SynchronizedCollection<ProcessInfoDto> processes)
        {
            ConcurrentDictionary<int, byte[]> list = new ConcurrentDictionary<int, byte[]>();
            lock (locker)
            {
                foreach (var process in processes)
                {
                    if (process.PID != default && process.ParentId is not null)
                    {
                        var ppid = Convert.ToInt32(process.ParentId);
                        var bytes = GetBytesFromPPID(ppid);

                        if (bytes is not null)
                            list.TryAdd(Convert.ToInt32(process.PID), bytes);
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
        internal bool CheckIfPIDExists(int pid)
        {
            if (IsComposeProcess(Process.GetProcessById(pid)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the given process is related to the main process.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public bool IsComposeProcess(object process)
        {
            try
            {
                var proc = process as Process;
                proc?.Refresh();

                if (proc is not null && ProcessMonitor.ComposePID != default)
                    while (proc?.Id != 0)
                    {
                        if (proc?.Id == ProcessMonitor.ComposePID)
                        {
                            return true;
                        }
                        else
                        {
                            int? ppid = GetParentId(proc);
                            if (ppid is not null)
                                proc = Process.GetProcessById(Convert.ToInt32(ppid));
                        } 
                    }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Sends a created process to publish
        /// </summary>
        /// <param name="ppid"></param>
        /// <param name="pid"></param>
        internal void SendNewDataIfPPIDExists(int ppid, int pid)
        {
            lock (locker)
            {
                if (CheckIfPIDExists(pid))
                {
                    var bytes = GetBytesFromPPID(ppid);
                    if (bytes is not null && ProcessIds is not null)
                        ProcessIds.AddOrUpdate(pid, bytes, (_,_) => bytes);
                    SendNewProcess?.Invoke(ProcessCreated(Process.GetProcessById(pid)));
                }
            }
        }

        /// <summary>
        /// Creates a byte array from PPID.
        /// </summary>
        /// <param name="ppid"></param>
        /// <returns></returns>
        private byte[] GetBytesFromPPID(int ppid)
        {
            var bytes = BitConverter.GetBytes(ppid);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// Sends a terminated process information to publish
        /// </summary>
        /// <param name="pid"></param>
        internal void SendDeletedDataPIDToCheckAsync(int pid)
        {
            if (ProcessIds is not null)
            {
                bool PIDExists = false;
                lock (locker)
                {
                    PIDExists = ProcessIds.ContainsKey(pid);
                }

                if (PIDExists)
                {
                    SendTerminatedProcess?.Invoke(pid);
                    lock (locker)
                    {
                        ProcessIds.TryRemove(pid, out byte[]? ppid);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a modified process information to publish
        /// </summary>
        /// <param name="pid"></param>
        internal void SendModifiedIfData(int pid)
        {
            lock (locker)
            {
                if(ProcessIds is not null)
                    if (ProcessIds.ContainsKey(pid))
                    {
                        SendModifiedProcess?.Invoke(pid);
                    }
            }
        }
    }
}
