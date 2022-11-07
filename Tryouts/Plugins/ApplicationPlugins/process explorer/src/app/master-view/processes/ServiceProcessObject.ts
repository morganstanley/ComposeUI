import { Subject } from "rxjs";
import { ConnectionInfo } from "../../DTOs/ConnectionInfo";
import { ModuleInfo } from "../../DTOs/ModuleInfo";
import { ProcessInfo } from "../../DTOs/ProcessInfo";
import { ProcessInfoCollectorData } from "../../DTOs/ProcessInfoCollectorData";
import { RegistrationInfo } from "../../DTOs/RegistrationInfo";

export class ServiceProcessObject{
    public processes:ProcessInfo[];
    public runtimeInfoOfProcesses:Map<string, ProcessInfoCollectorData> = new Map<string, ProcessInfoCollectorData>();

    public subjectAddProcesses: Subject<ProcessInfo[]> = new Subject<ProcessInfo[]>();
    public subjectAddProcess: Subject<ProcessInfo> = new Subject<ProcessInfo>();
    public subjectUpdateProcess: Subject<ProcessInfo> = new Subject<ProcessInfo>();
    public subjectRemoveProcess: Subject<ProcessInfo> = new Subject<ProcessInfo>();
    public subjectAddRuntimeInfo: Subject<ProcessInfoCollectorData> = new Subject<ProcessInfoCollectorData>();
    
    constructor() {
        
    }

    public AddProcesses(processes: ProcessInfo[]){
        this.processes = processes;
        this.subjectAddProcesses.next(this.processes);
        console.log("AddProcesses was called:", processes);
    }

    public AddProcess(process: ProcessInfo){
        if(this.getIndexOfProcess(process.PID) == -1){
            this.processes.push(process);
            this.subjectAddProcess.next(process);
        }
        console.log("Process has been created: ", process);
    }

    public UpdateProcess(process:ProcessInfo){
        var index = this.getIndexOfProcess(process.PID);
        if(index != -1){
            this.processes[index] = process;
            this.subjectAddProcesses.next(this.processes);
            this.subjectUpdateProcess.next(process);
        }
        console.log("Process has been modified: ", process);
    }

    public TerminateProcess(pid:number){
        console.log("PID: " + pid + " has been terminated");
        var index = this.getIndexOfProcess(pid);
        if(index >= 0){
            this.subjectRemoveProcess.next(this.processes[index]);
            this.processes.splice(index, 1);
        }
    }

    public AddConnections(assemblyId: string, conns:ConnectionInfo[]){
        console.log(conns);
    }

    public AddConnection(assemblyId: string, conn:ConnectionInfo){
        console.log(conn.Status);
    }

    public UpdateConnection(assemblyId: string, conn:ConnectionInfo){
        console.log(conn);
    }

    public UpdateEnvironmentVariables(assemblyId: string, environmentVariables:Map<string,string>){
        console.log(environmentVariables);
    }

    public UpdateRegistrations(assemblyId: string, registrations:RegistrationInfo[]){
        console.log(registrations);
    }

    public UpdateModules(assemblyId: string, modules:ModuleInfo[]){
        console.log(modules);
    }
    
    public AddRuntimeInfo(runtimeInfo: Map<string, ProcessInfoCollectorData>){
        console.log(runtimeInfo);
    }

    public AddRuntimeInfos(runtimeInfo: Map<string, ProcessInfoCollectorData>){
        console.log("Runtimeinfo initalized. ", runtimeInfo);
    }

    private getIndexOfProcess(pid : number) : number{
        return this.processes.findIndex(item => item.PID == pid);
    }

    public getProcesses() : ProcessInfo[]{
        return this.processes;
    }
}