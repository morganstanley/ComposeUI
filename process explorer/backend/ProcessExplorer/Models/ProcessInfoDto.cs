/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
using System.Diagnostics;

namespace ProcessMonitor.Models
{
    public class ProcessInfoDto
    {
        public DateTime? StartTime { get; set; }
        public TimeSpan? ProcessorUsage { get; set; }
        public long? PhysicalMemoryUsageB { get; set; }
        public string? ProcessName { get; set; } 
        public int? ProcessId { get; set; } 
        public int? PriorityLevel { get; set; } 
        public ProcessPriorityClass? ProcessPriorityClass { get; set; } 
        public ProcessThread[]? Threads { get;  set; }
        public long? VirtualMemorySize { get;  set; }
        public int? ParentId { get; set; } 
        public long? PagedMemoryUsage { get; set; } 
        public long? NonPagedMemoryUsage { get; set; }
        public long? PagedSystemMemoryUsage { get; set; } 
        public long? NonPagedSystemMemoryUsage { get; set; } 
        public long? PrivateMemoryUsage { get; set; }
        public TimeSpan? UserProcessorTime { get; set; }
        public object? Status { get; set; }
        public bool? IsLinux { get; set; }
        public List<ProcessInfoDto>? Children { get; set; }
        public float? MemoryUsagePercentage { get; set; }
        public float? CPUUsagePercentage { get; set; } 
    }
}