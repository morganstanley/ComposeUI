/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProcessExplorer.Entities
{
    public class ProcessMonitor
    {
        internal IProcessGenerator processInfoManager { get; set; }
        public ProcessMonitorDto Data { get; set; }
        public ProcessMonitor()
        {
            Data = new ProcessMonitorDto();
            if ((RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
                processInfoManager = new ProcessInfoLinux();
            else
                processInfoManager = new ProcessInfoWindows();
            ClearBag();
            FillBag();
        }

        public ProcessMonitor(List<ProcessInfoDto> processes)
            => Data.Processes = processes;

        private void ClearBag() => Data.Processes.Clear();
        private void FillBag()
        {
            Data.Processes.Add(new ProcessInfo(Process.GetCurrentProcess(), processInfoManager).Data);
            processInfoManager.WatchProcesses(processInfoManager.GetProcessIds(Data.Processes));
        }

        public List<ProcessInfoDto> GetProcesses() 
        {
            ClearBag();
            FillBag();
            return Data.Processes;
        }

        private IEnumerable<ProcessInfoDto> GetProcessByName(string name) => Data.Processes.Where(p => p.ProcessName == name);
        
        private IEnumerable<ProcessInfoDto> GetProcessByID(int id) => Data.Processes.Where(p => p.PID == id);
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
                throw new Exception(exception.Message);
            }
        }

        public void KillProcessByName(string processName)
        {
            KillProcess(processInfoManager.KillProcessByName(processName)).Wait();
        }

        public void KillProcessById(int processId)
        {
            KillProcess(processInfoManager.KillProcessById(processId)).Wait();
        }
    }

    public class ProcessMonitorDto
    {
        public List<ProcessInfoDto> Processes { get; set; } = new List<ProcessInfoDto>();
    }
}
