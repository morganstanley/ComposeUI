/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections.Concurrent;
using System.Diagnostics;

namespace ProcessExplorer.Processes
{
    public abstract class ProcessGeneratorBase
    {
        public event EventHandler<ProcessInfo>? SendNewProcess;
        public event EventHandler<int>? SendTerminatedProcess;
        public event EventHandler<int>? SendModifiedProcess;

        internal bool SendNewProcessAlreadyAdded = false;
        internal bool SendModifiedProcessAlreadyAdded = false;
        internal bool SendTerminatedProcessAlreadyAdded = false;

        internal ConcurrentDictionary<int, byte[]> ProcessIds = new ConcurrentDictionary<int, byte[]>();
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
        public abstract SynchronizedCollection<ProcessInfoData> GetChildProcesses(Process process);

        /// <summary>
        /// Kills a process by the given process name.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public abstract Process? KillProcessByName(string processName);

        /// <summary>
        /// Kills a process by the given process ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public abstract Process? KillProcessById(int processId);

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
        public abstract void WatchProcesses(SynchronizedCollection<ProcessInfoData> processes);

        /// <summary>
        /// It will add all of the the children to the list which is containing the relevant processes
        /// </summary>
        public abstract void AddChildProcessesToList();

        /// <summary>
        /// Creates a list, containing the process ids, which are running under the main process.
        /// </summary>
        /// <param name="processes"></param>
        /// <returns></returns>
        public ConcurrentDictionary<int, byte[]> GetProcessIds(SynchronizedCollection<ProcessInfoData> processes)
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
        /// Sends a created process information to publish
        /// </summary>
        /// <param name="ppid"></param>
        /// <param name="pid"></param>
        internal void SendNewDataIfPPIDExists(int ppid, int pid)
        {

            if (CheckIfPIDExists(pid))
            {
                var bytes = GetBytesFromPPID(ppid);
                lock (locker)
                {
                    ProcessIds.AddOrUpdate(pid, bytes, (_, _) => bytes);
                }
                SendNewProcess?.Invoke(this, ProcessCreated(Process.GetProcessById(pid)));
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
            if (!ProcessIds.IsEmpty)
            {
                bool PIDExists;
                PIDExists = ProcessIds.ContainsKey(pid);


                if (PIDExists)
                {
                    SendTerminatedProcess?.Invoke(this, pid);
                    lock (locker)
                    {
                        ProcessIds.TryRemove(pid, out var _);
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
                if (ProcessIds.ContainsKey(pid))
                {
                    SendModifiedProcess?.Invoke(this, pid);
                }
            }
        }
    }
}
