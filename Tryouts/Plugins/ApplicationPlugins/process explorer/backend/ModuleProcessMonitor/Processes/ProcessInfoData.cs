// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Diagnostics;
using ModuleProcessMonitor.Processes;

namespace ProcessExplorer.Abstraction.Processes;

public struct ProcessInfoData
{
    public ProcessInfoData(
        string? startTime,
        TimeSpan? processorUsageTime,
        long? physicalMemoryUsageBit,
        string? processName,
        int pID,
        int? priorityLevel,
        string? processPriorityClass,
        IEnumerable<ProcessThread> threads,
        long? virtualMemorySize,
        int? parentId,
        long? privateMemoryUsage,
        string? processStatus,
        float? memoryUsage,
        float? processorUsage)
    {
        StartTime = startTime;
        ProcessorUsageTime = processorUsageTime;
        PhysicalMemoryUsageBit = physicalMemoryUsageBit;
        ProcessName = processName;
        PID = pID;
        PriorityLevel = priorityLevel;
        ProcessPriorityClass = processPriorityClass;
        Threads = threads;
        VirtualMemorySize = virtualMemorySize;
        ParentId = parentId;
        PrivateMemoryUsage = privateMemoryUsage;
        ProcessStatus = processStatus;
        MemoryUsage = memoryUsage;
        ProcessorUsage = processorUsage;
    }

    public string? StartTime { get; set; }
    public TimeSpan? ProcessorUsageTime { get; set; }
    public long? PhysicalMemoryUsageBit { get; set; }
    public string? ProcessName { get; set; }
    public int PID { get; set; }
    public int? PriorityLevel { get; set; }
    public string? ProcessPriorityClass { get; set; }
    public IEnumerable<ProcessThread> Threads { get; set; } = Enumerable.Empty<ProcessThread>();
    public long? VirtualMemorySize { get; set; }
    public int? ParentId { get; set; }
    public long? PrivateMemoryUsage { get; set; }
    public string? ProcessStatus { get; set; } = Status.Running.ToStringCached();
    public float? MemoryUsage { get; set; }
    public float? ProcessorUsage { get; set; }
}
