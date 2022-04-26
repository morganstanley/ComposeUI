/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Diagnostics;
using ThreadState = System.Diagnostics.ThreadState;

namespace ProcessExplorer.Processes
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ProcessThreadInfo
    {
        public string? StartTime { get; internal set; }
        public int? PriorityLevel { get; internal set; }
        public int? Id { get; internal set; }
        public string? Status { get; internal set; }
        public TimeSpan? ProcessorUsageTime { get; internal set; }
        public string? WaitReason { get; internal set; }

        internal static ProcessThreadInfo FromProcessThread(ProcessThread processThread)
        {
            var Data = new ProcessThreadInfo();
            try
            {
                if (processThread.ThreadState == ThreadState.Wait)
                {
                    Data.WaitReason = processThread.WaitReason.ToStringCached();
                }
            }
            finally
            {
                Data.PriorityLevel = processThread.CurrentPriority;
                Data.Id = processThread.Id;
                Data.StartTime = processThread.StartTime.ToString("yyyy.MM.dd. hh:mm:s");
                Data.Status = processThread.ThreadState.ToStringCached();
                Data.ProcessorUsageTime = processThread.TotalProcessorTime;
            }

            return Data;
        }
    }
}
