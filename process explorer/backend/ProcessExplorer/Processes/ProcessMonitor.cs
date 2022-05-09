/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes.Logging;

namespace ProcessExplorer.Processes
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProcessMonitor : IProcessMonitor
    {
        #region Properties

        /// <summary>
        /// Main process ID, with we can recognize the related processes.
        /// </summary>
        public static int ComposePID { get; set; }

        /// <summary>
        /// Delay time for keeping the process in the list of processes after it is terminated. (seconds)
        /// Default: 1 minute.
        /// </summary>
        internal static int DelayTime = 60000;

        /// <summary>
        /// It contains the important information,that we need to pass.
        /// </summary>
        public ProcessMonitorInfo Data { get; } = new ProcessMonitorInfo();

        /// <summary>
        /// OS based handler, which will generate OS specific information related to the given process.
        /// </summary>
        private ProcessInfoManager? processInfoManager { get; }

        /// <summary>
        /// Logger instance to logging out messages.
        /// </summary>
        private readonly ILogger<ProcessMonitor>? logger;

        /// <summary>
        /// Lock object to gurantee thread safety.
        /// </summary>
        private readonly object locker = new object();


        /// <summary>
        /// Sample communicators, what we can use for sending object.
        /// </summary>
        public event EventHandler<int> processTerminatedAction;
        public event EventHandler<ProcessInfoData> processCreatedAction;
        public event EventHandler<ProcessInfoData> processModifiedAction;
        public event EventHandler<SynchronizedCollection<ProcessInfoData>> processesModifiedAction;

        #endregion

        #region Constructors

        public ProcessMonitor(ProcessInfoManager processInfoGenerator, ILogger<ProcessMonitor>? logger, int composePID)
            : this(processInfoGenerator, logger)
        {
            SetComposePID(composePID);
        }

        public ProcessMonitor(ProcessInfoManager processInfoGenerator, int composePID)
            : this(processInfoGenerator, null, composePID)
        {

        }

        public ProcessMonitor(ProcessInfoManager processInfoGenerator, ILogger<ProcessMonitor>? logger)
        {
            this.processInfoManager = processInfoGenerator;
            this.logger = logger;

            SetActionsIfTheyAreNotDeclared();
            ClearList();
            FillListWithRelatedProcesses();
            SetWatcher();

        }

        public ProcessMonitor(ProcessInfoManager processInfoGenerator, ILogger<ProcessMonitor>? logger, SynchronizedCollection<ProcessInfoData>? processes)
            : this(processInfoGenerator, logger)
        {
            if (processes is not null)
            {
                Data.Processes = processes;
            }
        }

        #endregion

        #region Setters

        /// <summary>
        /// Clears all of the element from the list.
        /// </summary>
        private void ClearList()
        {
            lock (locker)
            {
                Data.Processes.Clear();
            }
        }

        /// <summary>
        /// Sets the delay for keeping a process after it was terminated. (s)
        /// Default: 1 minute.
        /// </summary>
        /// <param name="delay"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SetDeadProcessRemovalDelay(int delay)
        {
            DelayTime = delay * 1000;
        }

        /// <summary>
        /// Fills the list with related processes to the Compose.
        /// </summary>
        public void FillListWithRelatedProcesses()
        {
            if (processInfoManager != null && ComposePID != default)
            {
                ClearList();
                try
                {
                    var main = Process.GetProcessById(ComposePID);

                    var proc = new ProcessInfo(main, processInfoManager);
                    if (proc.Data != default)
                    {
                        lock (locker)
                        {
                            Data.Processes.Add(proc.Data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.CannotFillListError(ex);
                }

                processInfoManager?.AddChildProcessesToList();
                logger?.ProcessListIsInitializedDebug();
            }
        }


        /// <summary>
        /// It will set the events to watch if a process is changed.
        /// </summary>
        public void SetWatcher()
        {
            var list = processInfoManager?.GetProcessIds(Data.Processes);
            if (list != null)
            {
                processInfoManager?.WatchProcesses(Data.Processes);
            }
        }

        /// <summary>
        /// Sets Compose PID.
        /// </summary>
        /// <param name="pid"></param>
        public void SetComposePID(int pid)
        {
            ComposePID = pid;
        }

        /// <summary>
        /// Sets the delegate actions in the OS based process _processInfo generator.
        /// </summary>
        private void SetActionsIfTheyAreNotDeclared()
        {
            if (processInfoManager != null)
            {
                if (!processInfoManager.SendModifiedProcessAlreadyAdded)
                {
                    processInfoManager.SendModifiedProcess += SendModifiedProcess;
                    processInfoManager.SendModifiedProcessAlreadyAdded = true;
                }

                if (!processInfoManager.SendNewProcessAlreadyAdded)
                {
                    processInfoManager.SendNewProcess += SendNewProcess;
                    processInfoManager.SendNewProcessAlreadyAdded = true;
                }

                if (!processInfoManager.SendTerminatedProcessAlreadyAdded)
                {
                    processInfoManager.SendTerminatedProcess += SendTerminatedProcess;
                    processInfoManager.SendTerminatedProcessAlreadyAdded = true;
                }
            }
        }

        #endregion

        #region Getters

        public SynchronizedCollection<ProcessInfoData> GetProcesses()
        {
            ClearList();
            FillListWithRelatedProcesses();
            return Data.Processes;
        }

        internal IEnumerable<ProcessInfoData> GetProcessByName(string name)
        {
            return Data.Processes.Where(p => p.ProcessName == name);
        }

        internal IEnumerable<ProcessInfoData> GetProcessByID(int id)
        {
            return Data.Processes.Where(p => p.PID == id);
        }

        #endregion

        #region Kill/terminate process

        private void KillProcess(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Exception exception)
            {
                logger?.CannotKillProcessError(exception);
            }
        }

        /// <summary>
        /// Terminates a process by the given name.
        /// </summary>
        /// <param name="processName"></param>
        public void KillProcessByName(string processName)
        {
            Process? process = null;
            if (processInfoManager != null)
            {
                process = processInfoManager.KillProcessByName(processName);
            }

            if (process is not null)
            {
                KillProcess(process);
            }
        }

        /// <summary>
        /// Terminates a process by the given ID.
        /// </summary>
        /// <param name="processId"></param>
        public void KillProcessById(int processId)
        {
            Process? process = null;
            if (processInfoManager != null)
            {
                process = processInfoManager.KillProcessById(processId);
            }
            if (process is not null)
            {
                KillProcess(process);
            }
        }

        #endregion

        #region Delete terminated process from the list helper

        /// <summary>
        /// Searches for the list index of a given process
        /// and calls the publishing method for deleting an element, if it is relevant.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private bool TryDeleteMainProcesses(int pid)
        {
            try
            {
                ProcessInfoData? item;
                lock (locker)
                {
                    item = Data.Processes.Single(p => p.PID == pid);
                }

                if (item != default)
                {
                    logger?.ProcessTerminatedInformation(pid);
                    ModifyStatus(item);
                    processTerminatedAction.Invoke(this, pid);
                    RemoveAfterTimeout(item);
                    return true;
                }
            }
            catch (Exception exception)
            {
                logger?.CannotFindElementError(pid, exception);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Modifies status of a the process which have been removed/terminated.
        /// </summary>
        /// <param name="item"></param>
        private void ModifyStatus(ProcessInfoData item)
        {
            lock (locker)
            {
                var index = Data.Processes.IndexOf(item);
                Data.Processes[index].ProcessStatus = Status.Terminated.ToStringCached();
                Data.Processes[index].ProcessorUsage = 0;
                Data.Processes[index].PhysicalMemoryUsageBit = 0;
                Data.Processes[index].VirtualMemorySize = 0;
                Data.Processes[index].PrivateMemoryUsage = 0;
                Data.Processes[index].Threads = new SynchronizedCollection<ProcessThreadInfo>();
            }
        }

        /// <summary>
        /// Delays the removing process from the list.
        /// </summary>
        /// <param name="item"></param>
        private void RemoveAfterTimeout(ProcessInfoData item)
        {
            Task.Run(async () =>
            {
                await Task.Delay(DelayTime);
                lock (locker)
                {
                    Data.Processes.Remove(item);
                    processesModifiedAction.Invoke(this, Data.Processes);
                }
            });
        }

        #endregion

        #region Send process changed helper

        /// <summary>
        /// Sends a message about creation of a process.
        /// </summary>
        /// <param name="process"></param>
        private void SendNewProcess(object? sender, ProcessInfo process)
        {
            var pid = Convert.ToInt32(process.Data?.PID);
            var temp = GetCopyList(pid);

            if (temp.Count == 0 && process.Data != default)
            {
                if (!CheckIfPIDExists(pid))
                {
                    Data.Processes.Add(process.Data);
                }
                    
                processCreatedAction.Invoke(this, process.Data);

                logger?.ProcessCreatedInformation(pid);
            }
        }

        /// <summary>
        /// Sends a message about termination of a process.
        /// </summary>
        /// <param name="pid"></param>
        private void SendTerminatedProcess(object? sender, int pid)
        {
            if (!TryDeleteMainProcesses(pid))
            {
                logger?.ProcessNotFoundError(pid);
            }
        }

        /// <summary>
        /// Sends a message about modification of a process and calls the modfier methods.
        /// </summary>
        /// <param name="pid"></param>
        private void SendModifiedProcess(object? sender, int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Refresh();
                if (process.Id > 0 && processInfoManager is not null)
                {
                    var processInfo = new ProcessInfo(process, processInfoManager);
                    ModifyElement(processInfo);
                    if (processInfo.Data is not null)
                    {
                        processModifiedAction.Invoke(this, processInfo.Data);
                    }

                    logger?.ProcessModifiedDebug(pid);
                }
            }
            catch (Exception exception)
            {
                logger?.CouldNotFoundModifiableProcessError(pid, exception);
            }
        }

        #endregion

        #region Private helper methods

        /// <summary>
        /// Creates a copy of the base list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private List<ProcessInfoData> GetCopyList(int pid)
        {
            lock (locker)
            {
                return Data.Processes.Where(p => p.PID == pid).ToList();
            }
        }

        /// <summary>
        /// Checks if the process ID can be found in the list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private bool CheckIfPIDExists(int pid)
        {
            try
            {
                int count;
                lock (locker)
                {
                    count = Data.Processes.Where(p => p.PID.Equals(pid)).Count();
                }

                if (count > 0)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                logger?.PIDNotExistsError(pid, exception);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Try to get an index of a specific process id from the list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private int? TryGetIndex(int pid)
        {
            try
            {
                lock (locker)
                {
                    var item = Data.Processes.FirstOrDefault(p => p.PID == pid);
                    if (item is not null)
                    {
                        return Data.Processes.IndexOf(item);
                    }
                }
            }
            catch (Exception exception)
            {
                logger?.IndexDoesNotExistsError(pid, exception);
            }

            return -1;
        }

        /// <summary>
        /// Modifies an element inside the list of processes, if this process is changed.
        /// </summary>
        /// <param name="processInfo"></param>
        private void ModifyElement(ProcessInfo processInfo)
        {
            int pid = Convert.ToInt32(processInfo.Data?.PID);
            if (processInfo.Data is not null && pid != default)
            {
                int? index = TryGetIndex(pid);
                if (index != -1)
                {
                    lock (locker)
                    {
                        var element = Data.Processes[Convert.ToInt32(index)];
                        if (element.ProcessorUsage != processInfo.Data.ProcessorUsage
                            && processInfo.Data.ProcessorUsage > 0)
                        {
                            element = processInfo.Data;
                        }
                        else
                        {
                            element.ProcessStatus = processInfo.Data.ProcessStatus;
                            element.MemoryUsage = processInfo.Data.MemoryUsage;
                            element.PhysicalMemoryUsageBit = processInfo.Data.PhysicalMemoryUsageBit;
                            element.PrivateMemoryUsage = processInfo.Data.PrivateMemoryUsage;
                            element.ProcessorUsageTime = processInfo.Data.ProcessorUsageTime;
                            element.Threads = processInfo.Data.Threads;
                            element.VirtualMemorySize = processInfo.Data.VirtualMemorySize;
                        }
                    }
                }  
            }
        }
        #endregion
    }
}
