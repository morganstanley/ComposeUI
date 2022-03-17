/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Entities;
using ProcessExplorer.Processes.Logging;
using System.Diagnostics;
using System.Globalization;

namespace ProcessExplorer.Processes
{
    public class ProcessInfoLinux : ProcessGeneratorBase
    {
        private readonly object locker = new object();
        private ILogger<ProcessInfoLinux>? logger;

        public ProcessInfoLinux(ILogger<ProcessInfoLinux> logger)
        {
            this.logger = logger;
        }

        public ProcessInfoLinux(Action<ProcessInfo> SendNewProcess, Action<int> SendTerminatedProcess, Action<int> SendModifiedProcess, ILogger<ProcessInfoLinux>? logger = null)
        {
            this.SendNewProcess = SendNewProcess;
            this.SendTerminatedProcess = SendTerminatedProcess;
            this.SendModifiedProcess = SendModifiedProcess;
            this.logger = logger;
        }

        private string RunLinuxPSCommand(Process process, string command)
        {
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
            lock (locker)
            {
                cli.Start();
                result = cli.StandardOutput.ReadToEnd();
                cli.Close();
                return result;
            }
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

        public string[]? GetLinuxInfo(int id)
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

        public override int? GetParentId(Process? child)
        {
            if(child is not null)
            {
                string[]? parts = GetLinuxInfo(child.Id);
                if (parts?.Length >= 3 && parts != default) return Convert.ToInt32(parts[2]);
                return default;
            }
            return default;
        }

        public override SynchronizedCollection<ProcessInfoDto> GetChildProcesses(Process parent)
        {
            SynchronizedCollection<ProcessInfoDto> children = new SynchronizedCollection<ProcessInfoDto>();

            Process[] processes = Process.GetProcesses(Environment.MachineName);
            lock (locker)
            {
                foreach (Process process in processes)
                {
                    int? ppid = GetParentId(process);
                    if (ppid == parent.Id && ppid != default)
                    {
                        var child = new ProcessInfo(process, this);
                        if (child != default && child.Data != default)
                            lock (locker)
                            {
                                children.Add(child.Data);
                            }  
                    }
                }
            }
            return children;
        }

        public override ProcessStartInfo KillProcessByName(string processName)
            => new ProcessStartInfo("/bin/bash", string.Format(" -c 'sudo pkill -f {0}'", processName));

        public override ProcessStartInfo KillProcessById(int processId)
            => new ProcessStartInfo("/bin/bash", string.Format(" -c 'sudo pkill -f {0}'", processId.ToString()));

        public override void WatchProcesses(SynchronizedCollection<ProcessInfoDto> processes)
        {
            using var watcher = new FileSystemWatcher(@"/proc");

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            string value = SplitPath(e.FullPath);
            if (CheckIfItsNumber(value))
            {
                int pid = Convert.ToInt32(value);
                int ppid = Convert.ToInt32(GetParentId(Process.GetProcessById(pid)));
                SendNewDataIfPPIDExists(ppid, pid);
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            string value = SplitPath(e.FullPath);
            if (CheckIfItsNumber(value))
            {
                int pid = Convert.ToInt32(value);
                SendDeletedDataPIDToCheckAsync(pid);
            }
        }

        private void ProcessModifiedAction(string value)
        {
            if (CheckIfItsNumber(value))
            {
                int pid = Convert.ToInt32(value);
                SendModifiedIfData(pid);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            string value = SplitPath(e.FullPath);
            ProcessModifiedAction(value);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            string value = SplitPath(e.FullPath);
            logger?.ProcessRenamed(e.OldFullPath, value);
            ProcessModifiedAction(value);
        }

        private void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                logger?.CannotSetWatcherLinux(ex);
                PrintException(ex.InnerException);
            }
        }

        private string SplitPath(string pathString)
            => pathString.Split("/")[2];

        private bool CheckIfItsNumber(string pid)
        {
            try
            {
                int convertedPID = Convert.ToInt32(pid);
                return true;
            }
            catch (Exception exception)
            {
                logger?.CouldNotConvertToInt(pid, exception);
                return false;
            }
        }
    }
}
