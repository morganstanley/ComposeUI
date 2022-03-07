/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Processes;
using System.Diagnostics;
using ProcessExplorer.Processes;

namespace ProcessExplorer.Entities
{

    public class ProcessInfo
    {
        public IProcessGenerator infoGenerator;
        private readonly object locker = new object();

        public ProcessInfo(int processId, IProcessGenerator manager)
            : this(Process.GetProcessById(processId), manager)
        {

        }

        public ProcessInfo(Process process, IProcessGenerator manager)
        {
            Data = new ProcessInfoDto();
            infoGenerator = manager;
            process.Refresh();
            if (process != null && process.Id != 0)
            {
                try
                {
                    Data.StartTime = process.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
                    Data.ProcessorUsageTime = process.TotalProcessorTime;
                    Data.PhysicalMemoryUsageBit = process.WorkingSet64;
                    Data.ProcessPriorityClass = process.PriorityClass.ToStringCached();
                    Data.VirtualMemorySize = process.VirtualMemorySize64;

                    var list = new SynchronizedCollection<ProcessThreadInfoDto>();
                    lock (locker)
                    {
                        int i;
                        for (i = 0; i < process.Threads.Count; i++)
                        {
                            try
                            {
                                list.Add(ProcessThreadInfoDto.FromProcessThread(process.Threads[i]));
                            }
                            catch(Exception exception)
                            {
                                Debug.WriteLine(string.Format("Cannot add thread to the list: {0}", exception.Message));
                            }
                        }
                    }

                    Data.Threads = list;
                    Data.ProcessStatus = process.HasExited == false ? Status.Running.ToStringCached() : Status.Stopped.ToStringCached();
                }
                catch (Exception)
                {
                    Data.ProcessStatus = Status.Stopped.ToStringCached();
                }
                finally
                {
                    Data.PID = process.Id;
                    Data.ProcessName = process.ProcessName;
                    Data.PriorityLevel = process.BasePriority;
                    Data.PrivateMemoryUsage = process.PrivateMemorySize64;
                    //Data.Children = infoGenerator.GetChildProcesses(process);
                    Data.ParentId = infoGenerator.GetParentId(process);
                    Data.MemoryUsage = infoGenerator.GetMemoryUsage(process);
                    Data.ProcessorUsage = infoGenerator.GetCPUUsage(process);
                }
            }
        }
        public ProcessInfoDto? Data { get; set; }
    }

}
