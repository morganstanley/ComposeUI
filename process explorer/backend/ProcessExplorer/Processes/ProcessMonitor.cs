/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using LocalCollector.RPCCommunicator;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes;
using ProcessExplorer.Processes.RPCCommunicator;
using System.Diagnostics;

namespace ProcessExplorer.Entities
{
    public class ProcessMonitor : IProcessMonitor
    {
        #region Properties
        /// <summary>
        /// Main process ID, with we can recognize the related processes.
        /// </summary>
        public static int ComposePID { get; set; }

        /// <summary>
        /// Url, where we can push the changes (if a process is created/modified/terminated).
        /// </summary>
        internal static string? ProcessChangedPushingUrl { get; set; }

        /// <summary>
        /// It contains the important information, we need to pass.
        /// </summary>
        public ProcessMonitorDto Data { get; set; } = new ProcessMonitorDto();

        /// <summary>
        /// OS based handler, which will generate OS specific information related to the given process.
        /// </summary>
        internal IProcessGenerator? processInfoManager { get; set; }

        /// <summary>
        /// Logger instance to log out messages.(DI works)
        /// </summary>
        private readonly ILogger<ProcessMonitor>? logger;

        /// <summary>
        /// Lock object to gurantee thread safety.
        /// </summary>
        private readonly object locker = new object();

        
        //Sample for later might not needed
        private readonly HttpClient? httpClient = new HttpClient();


        /// <summary>
        /// Sample communicator, what we can use for sending object, later it might be modified.
        /// </summary>
        private ICommunicator? channel;
        #endregion

        #region Constructors
        ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, ICommunicator? communicator)
        {
            this.processInfoManager = processInfoGenerator;
            this.logger = logger;

            if (communicator is not null)
                this.channel = communicator;

            SetProcessInfoManager();
            SetActionsIfTheyAreNotDeclared();
            ClearList();
            FillListWithRelatedProcesses();

            SetWatcher();
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, ICommunicator? communicator, int composePID)
            : this(processInfoGenerator, logger, communicator)
        {
            ComposePID = composePID;
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ICommunicator? communicator, int composePID)
            : this(processInfoGenerator, null, communicator)
        {
            ComposePID = composePID;
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, ICommunicator? communicator, int composePID, string url = "")
            : this(processInfoGenerator, logger, communicator, composePID)
        {
            ProcessChangedPushingUrl = url;
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger)
            : this(processInfoGenerator, logger, null)
        {

        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, ICommunicator? communicator, string? url = "") 
            : this(processInfoGenerator, logger, communicator)
        {
            ProcessChangedPushingUrl = url;
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, ICommunicator? communicator, SynchronizedCollection<ProcessInfoDto>? processes, string? url = "")
            : this(processInfoGenerator, logger, communicator)
        {
            if (processes is not null)
                Data.Processes = processes;
            ProcessChangedPushingUrl = url;
        }
        #endregion

        #region Setters
        /// <summary>
        /// Clears all of the element from the list of this instance.
        /// </summary>
        private void ClearList()
            => Data.Processes.Clear();

        /// <summary>
        /// Fills the list with related processes.
        /// </summary>
        public void FillListWithRelatedProcesses()
        {
            if (processInfoManager != null && ComposePID != default)
            {
                ClearList();
                var main = Process.GetProcessById(ComposePID);
                lock (locker)
                {
                    var proc = new ProcessInfo(main, processInfoManager);
                    if (Data != default && proc.Data != default && Data != default)
                    {
                        Data.Processes.Add(proc.Data);
                    }
                }
                processInfoManager?.AddChildProcessesToList();
                logger?.LogInformation("The Process Explorer list is initalized");
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
        /// Sets an url, where we can continuouosly push data.
        /// </summary>
        /// <param name="url"></param>
        public void SetSubribeUrl(string url)
            => ProcessChangedPushingUrl = url;

        /// <summary>
        /// Sets Compose PID.
        /// </summary>
        /// <param name="pid"></param>
        public void SetComposePID(int pid)
            => ComposePID = pid;

        /// <summary>
        /// Sets the delegates to the OS based process info generator.
        /// </summary>
        protected void SetActionsIfTheyAreNotDeclared()
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

        /// <summary>
        /// Sets OS based process info generator.
        /// </summary>
        protected void SetProcessInfoManager()
        {
            if (processInfoManager == null)
                processInfoManager = ProcessInfoManagerFactory.SetProcessInfoGeneratorBasedOnOS(SendNew, SendDeleted, SendModified);
        }
        #endregion

        #region Getters
        public SynchronizedCollection<ProcessInfoDto>? GetProcesses()
        {
            ClearList();
            FillListWithRelatedProcesses();
            return Data?.Processes;
        }

        internal IEnumerable<ProcessInfoDto>? GetProcessByName(string name)
            => Data?.Processes.Where(p => p.ProcessName == name);

        internal IEnumerable<ProcessInfoDto>? GetProcessByID(int id)
            => Data?.Processes.Where(p => p.PID == id);
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
                logger?.LogError(exception.Message);
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
        /// Searches for the index of the given process and calls the publishing method if it is relevant.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private async Task<bool> TryDeleteMainProcesses(int pid)
        {
            try
            {
                var item = Data.Processes.Single(p => p.PID == pid);
                if (item != default)
                {
                    await ProcessStatusChanged(pid);
                    logger?.LogInformation(string.Format("A process with PID: {0} is terminated", pid));
                    DeleteFromListAfter1Minute(item);
                    return true;
                }
            }
            catch (Exception exception)
            {
                logger?.LogError(exception.Message);
                return false;
            }
            return false;
        }

        /// <summary>
        /// Delays the removing process from the list.
        /// </summary>
        /// <param name="item"></param>
        private async void DeleteFromListAfter1Minute(ProcessInfoDto item)
        {
            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(60000);
                lock (locker)
                {
                    Data.Processes.Remove(item);
                }
            });
        }
        #endregion

        #region Send process changed helper
        /// <summary>
        /// Sets the message router publishing method.
        /// </summary>
        /// <param name="communicator"></param>
        public void SetCommunicator(ICommunicator communicator)
            => channel = communicator;

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="changedProcess"></param>
        /// <returns></returns>
        private async Task ProcessStatusChanged(object changedProcess)
        {
            if (channel is not null && channel.State is not CommunicatorState.Closed && channel.State is not null)
                await channel.SendMessage(changedProcess);
        }

        /// <summary>
        /// Sends a message about creation of a process.
        /// </summary>
        /// <param name="process"></param>
        public async void SendNew(ProcessInfo process)
        {
            var temp = GetCopyList(Convert.ToInt32(process?.Data?.PID));
            if (temp != null)
                if (temp?.Count == 0 && process?.Data != default)
                {
                    if (!CheckIfPIDExists(Convert.ToInt32(process.Data.PID)))
                        Data?.Processes.Add(process.Data);
                    await ProcessStatusChanged(process.Data);
                }
            logger?.LogInformation(string.Format("A process {0} is created under the Compose module.", process?.Data?.PID));
        }

        /// <summary>
        /// Sends a message about termination of a process.
        /// </summary>
        /// <param name="pid"></param>
        public void SendDeleted(int pid)
        {
            if (!TryDeleteMainProcesses(pid).Result)
                logger?.LogInformation(string.Format("The process {0} is not exist in the ProcessMonitor list", pid));
        }

        /// <summary>
        /// Sends a message about modification of a process.
        /// </summary>
        /// <param name="pid"></param>
        public async void SendModified(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Refresh();
                if (process != null && process.Id > 0 && processInfoManager != default)
                {
                    var processInfo = new ProcessInfo(process, processInfoManager);
                    ModifyElement(processInfo);
                    if (processInfo != null && processInfo.Data != null)
                        await ProcessStatusChanged(processInfo.Data);
                }
                logger?.LogInformation(string.Format("The process {0} is modified", pid));
            }
            catch (Exception exception)
            {
                logger?.LogError(exception.Message);
            }

        }
        #endregion

        #region Private helper methods
        /// <summary>
        /// Creates a copy of the base list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private List<ProcessInfoDto>? GetCopyList(int pid)
        {
            if (Data.Processes != default)
                return Data?.Processes.Where(p => p.PID == pid).ToList();
            return default;
        }

        /// <summary>
        /// Checks if the process ID can be found in the list.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private bool CheckIfPIDExists(int pid)
        {
            lock (locker)
            {
                try
                {
                    var count = Data.Processes.Where(p => p.PID.Equals(pid)).Count();
                    if (count > 0)
                        return true;
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception?.Message);
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Try to get a specific process id from the list. (first elements)
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private int TryGetIndexFromMainProcesses(int pid)
        {
            lock (locker)
            {
                try
                {
                    var index = Data.Processes.IndexOf(
                        Data.Processes.Where(p => p.PID == pid).FirstOrDefault());
                    return index;
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception.Message);
                }
            }
            return -1;
        }

        /// <summary>
        /// Modifies a related element inside the list, if a process is changed. //innerlist
        /// </summary>
        /// <param name="processInfo"></param>
        private void ModifyElement(ProcessInfo processInfo)
        {
            int pid = Convert.ToInt32(processInfo?.Data?.PID);
            if (processInfo?.Data is not null && pid != default)
            {
                int index = TryGetIndexFromMainProcesses(pid);
                if (index != -1 && processInfo != default)
                    lock (locker)
                        Data.Processes[index] = processInfo.Data;
            }
        }
        #endregion
    }
}
