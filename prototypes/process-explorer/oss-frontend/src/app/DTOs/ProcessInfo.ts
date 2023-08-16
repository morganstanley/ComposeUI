export interface ProcessInfo{
    ProcessName: string;
    PID: number;
    ProcessStatus: string;
    StartTime: string;
    ProcessorUsage: string;
    PriorityLevel: number;
    VirtualMemorySize: number 
}

export interface ProcessTable extends ProcessInfo { 
    Children : ProcessInfo[]
}
