/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;

namespace ProcessExplorer.Processes
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProcessInfo
    {
        internal ProcessInfoManager infoGenerator;

        public ProcessInfoData? Data { get; internal set; }

        internal ProcessInfo(ProcessInfoManager manager, int processId)
            : this(processId, manager)
        {
        }

        internal ProcessInfo(ProcessInfoManager manager, Process process)
            : this(process.Id, manager)
        {
        }

        internal ProcessInfo(int processId, ProcessInfoManager manager)
            : this(Process.GetProcessById(processId), manager)
        {
        }

        internal ProcessInfo(Process process, ProcessInfoManager manager)
        {
            Data = new ProcessInfoData();
            infoGenerator = manager;

            process.Refresh();
            if (process.Id != 0)
            {
                try
                {
                    Data.StartTime = process.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
                    Data.ProcessorUsageTime = process.TotalProcessorTime;
                    Data.PhysicalMemoryUsageBit = process.WorkingSet64;
                    Data.ProcessPriorityClass = process.PriorityClass.ToStringCached();
                    Data.VirtualMemorySize = process.VirtualMemorySize64;

                    var list = new SynchronizedCollection<ProcessThreadInfo>();
                    for (int i = 0; i < process.Threads.Count; i++)
                    {
                        list.Add(ProcessThreadInfo.FromProcessThread(process.Threads[i]));
                    }

                    Data.Threads = list;
                    Data.ProcessStatus = process.HasExited == false ? Status.Running.ToStringCached() : Status.Stopped.ToStringCached();
                }
                catch
                {
                    Data.ProcessStatus = Status.Stopped.ToStringCached();
                }
                finally
                {
                    Data.PID = process.Id;
                    Data.ProcessName = process.ProcessName;
                    Data.PriorityLevel = process.BasePriority;
                    Data.PrivateMemoryUsage = process.PrivateMemorySize64;
                    Data.ParentId = infoGenerator.GetParentId(process);
                    Data.MemoryUsage = infoGenerator.GetMemoryUsage(process);
                    Data.ProcessorUsage = infoGenerator.GetCPUUsage(process);
                }
            }
        }
    }
}
