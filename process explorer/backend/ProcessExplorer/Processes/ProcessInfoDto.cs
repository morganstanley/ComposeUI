/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

namespace ProcessExplorer.Processes
{
    public class ProcessInfoDto
    {
        public string? StartTime { get; set; } = default;
        public TimeSpan? ProcessorUsageTime { get; set; } = default;
        public long? PhysicalMemoryUsageBit { get; set; } = default;
        public string? ProcessName { get; set; } = default;
        public int? PID { get; set; } = default;
        public int? PriorityLevel { get; set; } = default;
        public string? ProcessPriorityClass { get; set; } = default;
        public SynchronizedCollection<ProcessThreadInfoDto>? Threads { get; set; } = new SynchronizedCollection<ProcessThreadInfoDto>();
        public long? VirtualMemorySize { get; set; } = default;
        public int? ParentId { get; set; } = null;
        public long? PrivateMemoryUsage { get; set; } = default;
        public string? ProcessStatus { get; set; } = Status.Running.ToString();
        public SynchronizedCollection<ProcessInfoDto>? Children { get; set; } = new SynchronizedCollection<ProcessInfoDto>();
        public float? MemoryUsage { get; set; } = default;
        public float? ProcessorUsage { get; set; } = default;
    }
}
