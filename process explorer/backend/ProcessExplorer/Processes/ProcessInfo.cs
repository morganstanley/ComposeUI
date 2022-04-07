/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using ThreadState = System.Diagnostics.ThreadState;
using Microsoft.Extensions.Logging;
using ProcessExplorer.Processes.Logging;

namespace ProcessExplorer.Processes
{
    public class ProcessInfo
    {
        public ProcessGeneratorBase infoGenerator;
        private readonly ILogger<ProcessInfo>? logger;

        public ProcessInfoData? Data { get; internal set; }

        public ProcessInfo(ILogger<ProcessInfo> logger, ProcessGeneratorBase manager, int processId)
            : this(processId, manager)
        {
            this.logger = logger;
        }

        public ProcessInfo(ILogger<ProcessInfo> logger, ProcessGeneratorBase manager, Process process)
            : this(process.Id, manager)
        {
            this.logger = logger;
        }

        public ProcessInfo(int processId, ProcessGeneratorBase manager)
            : this(Process.GetProcessById(processId), manager)
        {

        }

        internal ProcessInfo(Process process, ProcessGeneratorBase manager)
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
                    int i;
                    for (i = 0; i < process.Threads.Count; i++)
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
