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