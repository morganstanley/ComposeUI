/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using ThreadState = System.Diagnostics.ThreadState;

namespace ProcessExplorer.Processes
{
    public class ProcessThreadInfo
    {
        public string? StartTime { get; internal set; } = default;
        public int? PriorityLevel { get; internal set; } = default;
        public int? Id { get; internal set; } = default;
        public string? Status { get; internal set; } = default;
        public TimeSpan? ProcessorUsageTime { get; internal set; } = default;
        public string? WaitReason { get; internal set; } = default;

        public static ProcessThreadInfo FromProcessThread(ProcessThread processThread)
        {
            var Data = new ProcessThreadInfo();
            if (processThread != null)
            {
                Data.StartTime = processThread.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
                Data.PriorityLevel = processThread.CurrentPriority;
                Data.Id = processThread.Id;
                Data.Status = processThread.ThreadState.ToStringCached();
                Data.ProcessorUsageTime = processThread.TotalProcessorTime;
                Data.WaitReason = processThread.WaitReason.ToStringCached();
            }
            return Data;
        }

        public static ProcessThreadInfo FromProcessThread(DateTime startTime, int currentPriority,
            int id, ThreadState status, TimeSpan cpuUsageTime, ThreadWaitReason? waitReason = null)
        {
            var Data = new ProcessThreadInfo();
            Data.StartTime = startTime.ToString("yyyy.MM.dd. hh:mm:s");
            Data.PriorityLevel = currentPriority;
            Data.Id = id;
            Data.Status = status.ToStringCached();
            Data.ProcessorUsageTime = cpuUsageTime;

            return Data;
        }

    }
}
