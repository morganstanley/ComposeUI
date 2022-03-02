/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes;
using System.Diagnostics;

namespace ProcessExplorer.Entities
{
    public class ProcessMonitor : IProcessMonitor
    {
        #region Properties
        public static int ComposePID { get; set; }
        public ProcessMonitorDto Data { get; set; } = new ProcessMonitorDto();
        internal string? ProcessChangedPushingUrl { get; set; }
        internal IProcessGenerator? processInfoManager { get; set; }
        private readonly ILogger<ProcessMonitor>? logger;

        //Sample for later might not needed
        private readonly HttpClient? httpClient;
        #endregion

        #region Constructors
        ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, HttpClient? httpClient)
        {
            processInfoManager = processInfoGenerator;
            SetProcessInfoManager();
            SetActions();
            this.logger = logger;
            this.httpClient = httpClient;
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger)
            : this(processInfoGenerator, logger, null)
        {
            
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, HttpClient? httpClient, string? url = "") 
            : this(processInfoGenerator, logger, httpClient)
        {
            ProcessChangedPushingUrl = url;
            ClearProcesses();
            FillListWithRelatedProcesses();
        }

        public ProcessMonitor(IProcessGenerator processInfoGenerator, ILogger<ProcessMonitor>? logger, HttpClient? httpClient, SynchronizedCollection<ProcessInfoDto> processes, string? url = "") 
            : this(processInfoGenerator, logger, httpClient)
        {
            Data.Processes = processes;
            ProcessChangedPushingUrl = url;
        }
        #endregion

        #region Fillers
        private void ClearProcesses()
            => Data.Processes.Clear();

        private void FillListWithRelatedProcesses()
        {
            if (processInfoManager != null && ComposePID != default)
            {
                var proc = new ProcessInfo(Process.GetProcessById(ComposePID), processInfoManager);
                if (Data != default && proc.Data != default && Data != default)
                {
                    Data.Processes.Add(proc.Data);
                    var list = processInfoManager.GetProcessIds(Data.Processes);
                    if (list != null)
                        processInfoManager.WatchProcesses(Data.Processes);
                }
            }
        }

        public void SetSubribeUrl(string url)
            => ProcessChangedPushingUrl = url;

        public void SetComposePID(int pid)
            => ProcessMonitor.ComposePID = pid;

        protected void SetActions()
        {
            if(processInfoManager != null)
            {
                if (processInfoManager.SendModifiedProcess == default)
                    processInfoManager.SendModifiedProcess = SendModified;
                if (processInfoManager.SendNewProcess == default)
                    processInfoManager.SendNewProcess = SendNew;
                if (processInfoManager.SendTerminatedProcess == default)
                    processInfoManager.SendTerminatedProcess = SendDeleted;
            }
        }

        protected void SetProcessInfoManager()
        {
            if(processInfoManager == null)
                processInfoManager = ProcessInfoManagerFactory.SetProcessInfoGeneratorBasedOnOS(SendNew, SendDeleted, SendModified);
        }

        #endregion

        #region Getters
        public SynchronizedCollection<ProcessInfoDto>? GetProcesses()
        {
            ClearProcesses();
            FillListWithRelatedProcesses();
            return Data?.Processes;
        }

        internal IEnumerable<ProcessInfoDto>? GetProcessByName(string name)
            => Data?.Processes.Where(p => p.ProcessName == name);

        internal IEnumerable<ProcessInfoDto>? GetProcessByID(int id)
            => Data?.Processes.Where(p => p.PID == id);
        #endregion

        #region Kill process
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

        public void KillProcessByName(string processName)
        {
            if (processInfoManager != null)
                KillProcess(processInfoManager.KillProcessByName(processName)).Wait();
        }

        public void KillProcessById(int processId)
        {
            if (processInfoManager != null)
                KillProcess(processInfoManager.KillProcessById(processId)).Wait();
        }
        #endregion

        #region Delete terminated process from the list helper
        private async Task<bool> TryDeleteMainProcesses(int pid)
        {
            try
            {
                var item = Data.Processes.Single(p => p.PID == pid);
                if (item != default)
                {
                    Data.Processes.Remove(item);
                    if (ProcessChangedPushingUrl != null)
                        await ProcessStatusChanged(pid);
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

        private async void TryDeleteChildProcesses(int pid)
        {
            try
            {
                lock (Data.Processes)
                {
                    foreach (var process in Data.Processes)
                    {
                        if (process.Children != default)
                            lock (process.Children)
                            {
                                foreach (var ch in process.Children)
                                {
                                    if (ch.PID == pid)
                                    {
                                        process.Children.Remove(ch);
                                    }
                                }
                            }
                    }
                }
                if (ProcessChangedPushingUrl != null && ProcessChangedPushingUrl != "")
                    await ProcessStatusChanged(pid);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception.Message);
            }
        }
        #endregion

        #region Send process changed helper

        //Sample pushing data if process changed
        private async Task ProcessStatusChanged(object changedProcess)
        {
            if(httpClient != default && httpClient.BaseAddress != null)
                await httpClient.PutAsJsonAsync("", changedProcess);
        }

        public async void SendNew(ProcessInfo process)
        {
            var temp = GetCopyList(Convert.ToInt32(process?.Data?.PID));
            if (temp != null)
                if (temp?.Count == 0 && process?.Data != default && ProcessChangedPushingUrl != "")
                {
                    lock (Data.Processes)
                    {
                        if (!CheckIfPIDExists(Convert.ToInt32(process.Data.PID)))
                            Data?.Processes.Add(process.Data);
                    }
                    await ProcessStatusChanged(process.Data);
                }
            logger?.LogInformation("A process is created");
        }

        public void SendDeleted(int pid)
        {
            if (!TryDeleteMainProcesses(pid).Result)
                TryDeleteChildProcesses(pid);
            logger?.LogInformation("A process is terminated");
        }

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
                logger?.LogInformation("A process is modified");
            }
            catch (Exception exception)
            {
                logger?.LogError(exception.Message);
            }
            
        }
        #endregion

        #region Private helper methods
        private List<ProcessInfoDto>? GetCopyList(int pid)
        {
            if (Data.Processes != default)
                return Data?.Processes.Where(p => p.PID == pid).ToList();
            return default;
        }

        private bool CheckIfPIDExists(int pid)
        {
            lock (Data.Processes)
            {
                try
                {
                    var count = Data.Processes.Where(p => p.PID.Equals(pid)).Count();
                    if (count > 0)
                        return true;
                    else
                    {
                        var list = Data.Processes.SelectMany(p => p.Children).ToList();
                        if (list.Where(p => p.PID == pid).Count() > 0)
                            return true;
                    }
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception?.Message);
                    return false;
                }
            }
            return false;
        }

        private int TryGetIndexFromMainProcesses(int pid)
        {
            lock (Data.Processes)
            {
                try
                {
                    var index = Data.Processes.IndexOf(
                        Data.Processes.Where(p => p.PID == pid).FirstOrDefault());
                    return index;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex.Message);
                }
            }
            return -1;
        }

        private (int first, int second) TryGetIndexFromChildProcesses(int pid)
        {
            (int, int) indexes = default;
            int processIndex = 0;
            lock (Data.Processes)
            {
                foreach (var processInfo in Data.Processes)
                {
                    if (processInfo.Children != default)
                    {
                        lock (processInfo.Children)
                        {
                            int childIndex = 0;
                            foreach (var child in processInfo.Children)
                            {
                                if (child.PID == pid)
                                {
                                    indexes = (processIndex, childIndex);
                                    return indexes;
                                }
                            }
                        }
                        processIndex++;
                    }
                }
            }
            
            return indexes;
        }

        private void ModifyElement(ProcessInfo processInfo)
        {
            int pid = Convert.ToInt32(processInfo?.Data?.PID);
            if (processInfo?.Data != null)
            {
                if (pid != default)
                {
                    int index = TryGetIndexFromMainProcesses(pid);
                    if (index != -1 && processInfo != default)
                        lock (Data.Processes)
                        {
                            Data.Processes[index] = processInfo.Data;
                        }  
                }
                else
                {
                    var indexes = TryGetIndexFromChildProcesses(pid);
                    if (indexes != default && Data?.Processes[indexes.first] != default &&
                        Data?.Processes[indexes.first].Children != default &&
                        Data.Processes[indexes.first]?.Children[indexes.second] != default)
                        lock (Data.Processes)
                        {
                            Data.Processes[indexes.first].Children[indexes.second] = processInfo.Data;
                        }
                }
            }
        }
        #endregion
    }

    public class ProcessMonitorDto
    {
        public SynchronizedCollection<ProcessInfoDto> Processes { get; set; } = new SynchronizedCollection<ProcessInfoDto>();
    }
}
