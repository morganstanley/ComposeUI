/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes.Communicator;
using ProcessExplorer.Processes.Logging;

namespace ProcessExplorer.Processes
{
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
        public static int DelayTime = 60000;

        /// <summary>
        /// It contains the important information,that we need to pass.
        /// </summary>
        public ProcessMonitorInfo Data { get; set; } = new ProcessMonitorInfo();

        /// <summary>
        /// OS based handler, which will generate OS specific information related to the given process.
        /// </summary>
        internal ProcessGeneratorBase? processInfoManager { get; set; }

        /// <summary>
        /// Logger instance to logging out messages.
        /// </summary>
        private readonly ILogger<ProcessMonitor>? logger;

        /// <summary>
        /// Lock object to gurantee thread safety.
        /// </summary>
        private readonly object locker = new object();


        /// <summary>
        /// Sample communicator, what we can use for sending object, later it might be modified.
        /// </summary>
        private IUIHandler? UICommunicator;
        #endregion

        #region Constructors
        ProcessMonitor(ProcessGeneratorBase processInfoGenerator, ILogger<ProcessMonitor>? logger, IUIHandler? communicator)
        {
            this.processInfoManager = processInfoGenerator;
            this.logger = logger;

            if (communicator is not null)
                this.UICommunicator = communicator;
            SetActionsIfTheyAreNotDeclared();
            ClearList();
            FillListWithRelatedProcesses();
            SetWatcher();
        }

        public ProcessMonitor(ProcessGeneratorBase processInfoGenerator, ILogger<ProcessMonitor>? logger, IUIHandler? communicator, int composePID)
            : this(processInfoGenerator, logger, communicator)
        {
            SetComposePID(composePID);
        }

        public ProcessMonitor(ProcessGeneratorBase processInfoGenerator, IUIHandler? communicator, int composePID)
            : this(processInfoGenerator, null, communicator, composePID)
        {

        }

        public ProcessMonitor(ProcessGeneratorBase processInfoGenerator, ILogger<ProcessMonitor>? logger)
            : this(processInfoGenerator, logger, null)
        {

        }

        public ProcessMonitor(ProcessGeneratorBase processInfoGenerator, ILogger<ProcessMonitor>? logger, IUIHandler? communicator, SynchronizedCollection<ProcessInfoData>? processes)
            : this(processInfoGenerator, logger, communicator)
        {
            if (processes is not null)
                Data.Processes = processes;
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
            lock (locker)
            {
                DelayTime = delay * 1000;
            }
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
                    lock (locker)
                    {
                        var proc = new ProcessInfo(main, processInfoManager);
                        if (proc.Data != default)
                        {
                            Data.Processes.Add(proc.Data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.CannotFillList(ex);
                }
                processInfoManager?.AddChildProcessesToList();
                logger?.ProcessListIsInitalized();

            }
        }


        /// <summary>
        /// It will set the events to watch if a process is changed.
        /// </summary>
        public void SetWatcher()
        {
            var list = processInfoManager?.GetProcessIds(Data.Processes);
            if (list != null)
                processInfoManager?.WatchProcesses(Data.Processes);
        }

        /// <summary>
        /// Sets Compose PID.
        /// </summary>
        /// <param name="pid"></param>
        public void SetComposePID(int pid)
        {
            lock (locker)
            {
                ComposePID = pid;
            }
        }

        /// <summary>
        /// Sets the delegate actions in the OS based process _processInfo generator.
        /// </summary>
        private void SetActionsIfTheyAreNotDeclared()
        {
            if (processInfoManager != null)
            {
                if (processInfoManager.SendModifiedProcess == default)
                    processInfoManager.SendModifiedProcess = SendModified;
                if (processInfoManager.SendNewProcess == default)
                    processInfoManager.SendNewProcess = SendNew;
                if (processInfoManager.SendTerminatedProcess == default)
                    processInfoManager.SendTerminatedProcess = SendDeleted;
            }
        }

        public void SetUICommunicatorToWatchProcessChanges(IUIHandler handler)
        {
            this.UICommunicator = handler;
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
            lock (locker)
            {
                return Data.Processes.Where(p => p.ProcessName == name);
            }
        }

        internal IEnumerable<ProcessInfoData> GetProcessByID(int id)
        {
            lock (locker)
            {
                return Data.Processes.Where(p => p.PID == id);
            }
        }
        #endregion

        #region Kill/terminate process
        private async Task KillProcess(ProcessStartInfo info)
        {
            try
            {
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
                logger?.CannotKillProcess(exception);
            }
        }

        /// <summary>
        /// Terminates a process by the given name.
        /// </summary>
        /// <param name="processName"></param>
        public void KillProcessByName(string processName)
        {
            if (processInfoManager != null)
                KillProcess(processInfoManager.KillProcessByName(processName)).Wait();
        }

        /// <summary>
        /// Terminates a process by the given ID.
        /// </summary>
        /// <param name="processId"></param>
        public void KillProcessById(int processId)
        {
            if (processInfoManager != null)
                KillProcess(processInfoManager.KillProcessById(processId)).Wait();
        }
        #endregion

        #region Delete terminated process from the list helper
        /// <summary>
        /// Searches for the list index of a given process
        /// and calls the publishing method for deleting an element, if it is relevant.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private async Task<bool> TryDeleteMainProcesses(int pid)
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
                    if (CheckCommunicatorState())
                    {
                        await UICommunicator.RemoveProcess(pid);
                    }

                    logger?.ProcessTerminated(pid);

                    ModifyStatus(item);
                    RemoveAfterTimeout(item);
                    return true;
                }
            }
            catch (Exception exception)
            {
                logger?.CannotFindElement(pid, exception);
                return false;
            }
            return false;
        }

        private void ModifyStatus(ProcessInfoData item)
        {
            lock (locker)
            {
                var index = Data.Processes.IndexOf(item);
                Data.Processes[index].ProcessStatus = Status.Terminated.ToStringCached();
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
                }
            });
        }
        #endregion

        #region Send process changed helper
        /// <summary>
        /// Sends a message about creation of a process.
        /// </summary>
        /// <param name="process"></param>
        public async void SendNew(ProcessInfo process)
        {
            var pid = Convert.ToInt32(process.Data?.PID);
            var temp = GetCopyList(pid);
            if (temp is not null)
            {
                if (temp?.Count == 0 && process?.Data != default)
                {
                    if (!CheckIfPIDExists(pid))
                        Data.Processes.Add(process.Data);
                    if (CheckCommunicatorState())
                    {
                        await UICommunicator?.AddProcess(process.Data);
                    }
                    logger?.ProcessCreated(pid);
                }
            }
        }

        /// <summary>
        /// Sends a message about termination of a process.
        /// </summary>
        /// <param name="pid"></param>
        public void SendDeleted(int pid)
        {
            if (!TryDeleteMainProcesses(pid).Result)
                logger?.ProcessNotFound(pid);
        }

        /// <summary>
        /// Sends a message about modification of a process and calls the modfier methods.
        /// </summary>
        /// <param name="pid"></param>
        public async void SendModified(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Refresh();
                if (process.Id > 0 && processInfoManager is not null)
                {
                    var processInfo = new ProcessInfo(process, processInfoManager);
                    ModifyElement(processInfo);
                    if (processInfo.Data is not null && CheckCommunicatorState())
                    {
                        await UICommunicator.UpdateProcess(processInfo.Data);
                    }
                    logger?.ProcessModified(pid);
                }
            }
            catch (Exception exception)
            {
                logger?.CouldNotFoundModifiableProcess(pid, exception);
            }

        }
        #endregion

        #region Private helper methods
        /// <summary>
        /// Creates a copy of the base list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private List<ProcessInfoData>? GetCopyList(int pid)
        {
            return Data.Processes.Where(p => p.PID == pid).ToList();
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
                lock (locker)
                {
                    var count = Data.Processes.Where(p => p.PID.Equals(pid)).Count();
                    if (count > 0)
                        return true;
                }
            }
            catch (Exception exception)
            {
                logger?.NotExists(pid, exception);
                return false;
            }
            return false;
        }

        /// <summary>
        /// Try to get an index of a specific process id from the list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private int TryGetIndex(int pid)
        {
            try
            {
                lock (locker)
                {
                    var item = Data.Processes.Where(p => p.PID == pid).FirstOrDefault();
                    if (item is not null)
                    {
                        return Data.Processes.IndexOf(item);
                    }
                }
            }
            catch (Exception exception)
            {
                logger?.IndexDoesNotExists(pid, exception);
            }

            return -1;
        }

        /// <summary>
        /// Modifies an element inside the list of processes, if this process is changed.
        /// </summary>
        /// <param name="processInfo"></param>
        private void ModifyElement(ProcessInfo processInfo)
        {
            int pid = Convert.ToInt32(processInfo?.Data?.PID);
            if (processInfo?.Data is not null && pid != default)
            {
                int index = TryGetIndex(pid);
                if (index != -1)
                    lock (locker)
                        Data.Processes[index] = processInfo.Data;
            }
        }

        /// <summary>
        /// Checks if the communicator is instantiated and the communicator channel is opened.
        /// </summary>
        /// <returns></returns>
        private bool CheckCommunicatorState()
            => UICommunicator is not null && UICommunicator.State == CommunicatorState.Opened;

        #endregion
    }
}
