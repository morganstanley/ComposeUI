/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

namespace ProcessExplorer.Processes
{
    public class ProcessInfoData
    {
        public string? StartTime { get; internal set; } = default;
        public TimeSpan? ProcessorUsageTime { get; internal set; } = default;
        public long? PhysicalMemoryUsageBit { get; internal set; } = default;
        public string? ProcessName { get; internal set; } = default;
        public int? PID { get; internal set; } = default;
        public int? PriorityLevel { get; internal set; } = default;
        public string? ProcessPriorityClass { get; internal set; } = default;
        public SynchronizedCollection<ProcessThreadInfo> Threads { get; internal set; } = new SynchronizedCollection<ProcessThreadInfo>();
        public long? VirtualMemorySize { get; internal set; } = default;
        public int? ParentId { get; internal set; } = default;
        public long? PrivateMemoryUsage { get; internal set; } = default;
        public string? ProcessStatus { get; internal set; } = Status.Running.ToStringCached();
        public float? MemoryUsage { get; internal set; } = default;
        public float? ProcessorUsage { get; internal set; } = default;
    }
}
