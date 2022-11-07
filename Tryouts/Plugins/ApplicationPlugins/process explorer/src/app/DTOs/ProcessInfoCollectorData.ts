import { ConnectionInfo } from "./ConnectionInfo";
import { ModuleInfo } from "./ModuleInfo";
import { RegistrationInfo } from "./RegistrationInfo";

export class ProcessInfoCollectorData{
    Id: number;
    Registrations: RegistrationInfo[] = new Array<RegistrationInfo>();
    EnvironmentVariables: Map<string, string> = new Map<string, string>();
    Connections: ConnectionInfo[]= new Array<ConnectionInfo>();
    Modules: ModuleInfo[] = new Array<ModuleInfo>();
}