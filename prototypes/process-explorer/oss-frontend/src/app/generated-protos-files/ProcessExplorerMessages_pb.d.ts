// package: 
// file: ProcessExplorerMessages.proto

import * as jspb from "google-protobuf";
import * as google_protobuf_duration_pb from "google-protobuf/google/protobuf/duration_pb";
import * as google_protobuf_empty_pb from "google-protobuf/google/protobuf/empty_pb";

export class Message extends jspb.Message {
  hasAction(): boolean;
  clearAction(): void;
  getAction(): ActionTypeMap[keyof ActionTypeMap];
  setAction(value: ActionTypeMap[keyof ActionTypeMap]): void;

  hasDescription(): boolean;
  clearDescription(): void;
  getDescription(): string;
  setDescription(value: string): void;

  hasAssemblyid(): boolean;
  clearAssemblyid(): void;
  getAssemblyid(): string;
  setAssemblyid(value: string): void;

  hasProcessid(): boolean;
  clearProcessid(): void;
  getProcessid(): number;
  setProcessid(value: number): void;

  getPeriodofdelay(): number;
  setPeriodofdelay(value: number): void;

  clearProcessesList(): void;
  getProcessesList(): Array<Process>;
  setProcessesList(value: Array<Process>): void;
  addProcesses(value?: Process, index?: number): Process;

  getProcessstatuschangesMap(): jspb.Map<number, string>;
  clearProcessstatuschangesMap(): void;
  hasRuntimeinfo(): boolean;
  clearRuntimeinfo(): void;
  getRuntimeinfo(): ProcessInfoCollectorData | undefined;
  setRuntimeinfo(value?: ProcessInfoCollectorData): void;

  getMultipleruntimeinfoMap(): jspb.Map<string, ProcessInfoCollectorData>;
  clearMultipleruntimeinfoMap(): void;
  clearConnectionsList(): void;
  getConnectionsList(): Array<Connection>;
  setConnectionsList(value: Array<Connection>): void;
  addConnections(value?: Connection, index?: number): Connection;

  getConnectionstatuschangesMap(): jspb.Map<string, string>;
  clearConnectionstatuschangesMap(): void;
  clearRegistrationsList(): void;
  getRegistrationsList(): Array<Registration>;
  setRegistrationsList(value: Array<Registration>): void;
  addRegistrations(value?: Registration, index?: number): Registration;

  clearModulesList(): void;
  getModulesList(): Array<Module>;
  setModulesList(value: Array<Module>): void;
  addModules(value?: Module, index?: number): Module;

  getEnvironmentvariablesMap(): jspb.Map<string, string>;
  clearEnvironmentvariablesMap(): void;
  getSubsystemsMap(): jspb.Map<string, Subsystem>;
  clearSubsystemsMap(): void;
  getAction1Case(): Message.Action1Case;
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Message.AsObject;
  static toObject(includeInstance: boolean, msg: Message): Message.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Message, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Message;
  static deserializeBinaryFromReader(message: Message, reader: jspb.BinaryReader): Message;
}

export namespace Message {
  export type AsObject = {
    action: ActionTypeMap[keyof ActionTypeMap],
    description: string,
    assemblyid: string,
    processid: number,
    periodofdelay: number,
    processesList: Array<Process.AsObject>,
    processstatuschangesMap: Array<[number, string]>,
    runtimeinfo?: ProcessInfoCollectorData.AsObject,
    multipleruntimeinfoMap: Array<[string, ProcessInfoCollectorData.AsObject]>,
    connectionsList: Array<Connection.AsObject>,
    connectionstatuschangesMap: Array<[string, string]>,
    registrationsList: Array<Registration.AsObject>,
    modulesList: Array<Module.AsObject>,
    environmentvariablesMap: Array<[string, string]>,
    subsystemsMap: Array<[string, Subsystem.AsObject]>,
  }

  export enum Action1Case {
    ACTION1_NOT_SET = 0,
    ACTION = 1,
  }
}

export class Process extends jspb.Message {
  hasStarttime(): boolean;
  clearStarttime(): void;
  getStarttime(): string;
  setStarttime(value: string): void;

  hasProcessorusagetime(): boolean;
  clearProcessorusagetime(): void;
  getProcessorusagetime(): google_protobuf_duration_pb.Duration | undefined;
  setProcessorusagetime(value?: google_protobuf_duration_pb.Duration): void;

  hasPhysicalmemoryusagebit(): boolean;
  clearPhysicalmemoryusagebit(): void;
  getPhysicalmemoryusagebit(): number;
  setPhysicalmemoryusagebit(value: number): void;

  hasProcessname(): boolean;
  clearProcessname(): void;
  getProcessname(): string;
  setProcessname(value: string): void;

  hasProcessid(): boolean;
  clearProcessid(): void;
  getProcessid(): number;
  setProcessid(value: number): void;

  hasProcesspriorityclass(): boolean;
  clearProcesspriorityclass(): void;
  getProcesspriorityclass(): string;
  setProcesspriorityclass(value: string): void;

  clearThreadsList(): void;
  getThreadsList(): Array<ProcessThreadInfo>;
  setThreadsList(value: Array<ProcessThreadInfo>): void;
  addThreads(value?: ProcessThreadInfo, index?: number): ProcessThreadInfo;

  hasVirtualmemorysize(): boolean;
  clearVirtualmemorysize(): void;
  getVirtualmemorysize(): number;
  setVirtualmemorysize(value: number): void;

  hasParentid(): boolean;
  clearParentid(): void;
  getParentid(): number;
  setParentid(value: number): void;

  hasPrivatememoryusage(): boolean;
  clearPrivatememoryusage(): void;
  getPrivatememoryusage(): number;
  setPrivatememoryusage(value: number): void;

  hasProcessstatus(): boolean;
  clearProcessstatus(): void;
  getProcessstatus(): string;
  setProcessstatus(value: string): void;

  hasMemoryusage(): boolean;
  clearMemoryusage(): void;
  getMemoryusage(): number;
  setMemoryusage(value: number): void;

  hasProcessorusage(): boolean;
  clearProcessorusage(): void;
  getProcessorusage(): number;
  setProcessorusage(value: number): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Process.AsObject;
  static toObject(includeInstance: boolean, msg: Process): Process.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Process, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Process;
  static deserializeBinaryFromReader(message: Process, reader: jspb.BinaryReader): Process;
}

export namespace Process {
  export type AsObject = {
    starttime: string,
    processorusagetime?: google_protobuf_duration_pb.Duration.AsObject,
    physicalmemoryusagebit: number,
    processname: string,
    processid: number,
    processpriorityclass: string,
    threadsList: Array<ProcessThreadInfo.AsObject>,
    virtualmemorysize: number,
    parentid: number,
    privatememoryusage: number,
    processstatus: string,
    memoryusage: number,
    processorusage: number,
  }
}

export class ProcessThreadInfo extends jspb.Message {
  hasStarttime(): boolean;
  clearStarttime(): void;
  getStarttime(): string;
  setStarttime(value: string): void;

  hasPrioritylevel(): boolean;
  clearPrioritylevel(): void;
  getPrioritylevel(): number;
  setPrioritylevel(value: number): void;

  hasId(): boolean;
  clearId(): void;
  getId(): number;
  setId(value: number): void;

  hasStatus(): boolean;
  clearStatus(): void;
  getStatus(): string;
  setStatus(value: string): void;

  hasProcessorusagetime(): boolean;
  clearProcessorusagetime(): void;
  getProcessorusagetime(): google_protobuf_duration_pb.Duration | undefined;
  setProcessorusagetime(value?: google_protobuf_duration_pb.Duration): void;

  hasWaitreason(): boolean;
  clearWaitreason(): void;
  getWaitreason(): string;
  setWaitreason(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ProcessThreadInfo.AsObject;
  static toObject(includeInstance: boolean, msg: ProcessThreadInfo): ProcessThreadInfo.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ProcessThreadInfo, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ProcessThreadInfo;
  static deserializeBinaryFromReader(message: ProcessThreadInfo, reader: jspb.BinaryReader): ProcessThreadInfo;
}

export namespace ProcessThreadInfo {
  export type AsObject = {
    starttime: string,
    prioritylevel: number,
    id: number,
    status: string,
    processorusagetime?: google_protobuf_duration_pb.Duration.AsObject,
    waitreason: string,
  }
}

export class ProcessInfoCollectorData extends jspb.Message {
  getId(): number;
  setId(value: number): void;

  clearRegistrationsList(): void;
  getRegistrationsList(): Array<Registration>;
  setRegistrationsList(value: Array<Registration>): void;
  addRegistrations(value?: Registration, index?: number): Registration;

  getEnvironmentvariablesMap(): jspb.Map<string, string>;
  clearEnvironmentvariablesMap(): void;
  clearConnectionsList(): void;
  getConnectionsList(): Array<Connection>;
  setConnectionsList(value: Array<Connection>): void;
  addConnections(value?: Connection, index?: number): Connection;

  clearModulesList(): void;
  getModulesList(): Array<Module>;
  setModulesList(value: Array<Module>): void;
  addModules(value?: Module, index?: number): Module;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ProcessInfoCollectorData.AsObject;
  static toObject(includeInstance: boolean, msg: ProcessInfoCollectorData): ProcessInfoCollectorData.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: ProcessInfoCollectorData, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ProcessInfoCollectorData;
  static deserializeBinaryFromReader(message: ProcessInfoCollectorData, reader: jspb.BinaryReader): ProcessInfoCollectorData;
}

export namespace ProcessInfoCollectorData {
  export type AsObject = {
    id: number,
    registrationsList: Array<Registration.AsObject>,
    environmentvariablesMap: Array<[string, string]>,
    connectionsList: Array<Connection.AsObject>,
    modulesList: Array<Module.AsObject>,
  }
}

export class Registration extends jspb.Message {
  hasImplementationtype(): boolean;
  clearImplementationtype(): void;
  getImplementationtype(): string;
  setImplementationtype(value: string): void;

  hasLifetime(): boolean;
  clearLifetime(): void;
  getLifetime(): string;
  setLifetime(value: string): void;

  hasServicetype(): boolean;
  clearServicetype(): void;
  getServicetype(): string;
  setServicetype(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Registration.AsObject;
  static toObject(includeInstance: boolean, msg: Registration): Registration.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Registration, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Registration;
  static deserializeBinaryFromReader(message: Registration, reader: jspb.BinaryReader): Registration;
}

export namespace Registration {
  export type AsObject = {
    implementationtype: string,
    lifetime: string,
    servicetype: string,
  }
}

export class Connection extends jspb.Message {
  hasId(): boolean;
  clearId(): void;
  getId(): string;
  setId(value: string): void;

  hasName(): boolean;
  clearName(): void;
  getName(): string;
  setName(value: string): void;

  hasLocalendpoint(): boolean;
  clearLocalendpoint(): void;
  getLocalendpoint(): string;
  setLocalendpoint(value: string): void;

  hasRemoteendpoint(): boolean;
  clearRemoteendpoint(): void;
  getRemoteendpoint(): string;
  setRemoteendpoint(value: string): void;

  hasRemoteapplication(): boolean;
  clearRemoteapplication(): void;
  getRemoteapplication(): string;
  setRemoteapplication(value: string): void;

  hasRemotehostname(): boolean;
  clearRemotehostname(): void;
  getRemotehostname(): string;
  setRemotehostname(value: string): void;

  getConnectioninformationMap(): jspb.Map<string, string>;
  clearConnectioninformationMap(): void;
  hasStatus(): boolean;
  clearStatus(): void;
  getStatus(): string;
  setStatus(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Connection.AsObject;
  static toObject(includeInstance: boolean, msg: Connection): Connection.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Connection, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Connection;
  static deserializeBinaryFromReader(message: Connection, reader: jspb.BinaryReader): Connection;
}

export namespace Connection {
  export type AsObject = {
    id: string,
    name: string,
    localendpoint: string,
    remoteendpoint: string,
    remoteapplication: string,
    remotehostname: string,
    connectioninformationMap: Array<[string, string]>,
    status: string,
  }
}

export class Module extends jspb.Message {
  hasName(): boolean;
  clearName(): void;
  getName(): string;
  setName(value: string): void;

  hasVersion(): boolean;
  clearVersion(): void;
  getVersion(): string;
  setVersion(value: string): void;

  hasVersionredirectedfrom(): boolean;
  clearVersionredirectedfrom(): void;
  getVersionredirectedfrom(): string;
  setVersionredirectedfrom(value: string): void;

  hasPublickeytoken(): boolean;
  clearPublickeytoken(): void;
  getPublickeytoken(): Uint8Array | string;
  getPublickeytoken_asU8(): Uint8Array;
  getPublickeytoken_asB64(): string;
  setPublickeytoken(value: Uint8Array | string): void;

  hasLocation(): boolean;
  clearLocation(): void;
  getLocation(): string;
  setLocation(value: string): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Module.AsObject;
  static toObject(includeInstance: boolean, msg: Module): Module.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Module, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Module;
  static deserializeBinaryFromReader(message: Module, reader: jspb.BinaryReader): Module;
}

export namespace Module {
  export type AsObject = {
    name: string,
    version: string,
    versionredirectedfrom: string,
    publickeytoken: Uint8Array | string,
    location: string,
  }
}

export class Subsystem extends jspb.Message {
  getName(): string;
  setName(value: string): void;

  hasStartuptype(): boolean;
  clearStartuptype(): void;
  getStartuptype(): string;
  setStartuptype(value: string): void;

  hasUitype(): boolean;
  clearUitype(): void;
  getUitype(): string;
  setUitype(value: string): void;

  hasPath(): boolean;
  clearPath(): void;
  getPath(): string;
  setPath(value: string): void;

  hasUrl(): boolean;
  clearUrl(): void;
  getUrl(): string;
  setUrl(value: string): void;

  clearArgumentsList(): void;
  getArgumentsList(): Array<string>;
  setArgumentsList(value: Array<string>): void;
  addArguments(value: string, index?: number): string;

  hasPort(): boolean;
  clearPort(): void;
  getPort(): number;
  setPort(value: number): void;

  getState(): string;
  setState(value: string): void;

  hasDescription(): boolean;
  clearDescription(): void;
  getDescription(): string;
  setDescription(value: string): void;

  getAutomatedstart(): boolean;
  setAutomatedstart(value: boolean): void;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Subsystem.AsObject;
  static toObject(includeInstance: boolean, msg: Subsystem): Subsystem.AsObject;
  static extensions: {[key: number]: jspb.ExtensionFieldInfo<jspb.Message>};
  static extensionsBinary: {[key: number]: jspb.ExtensionFieldBinaryInfo<jspb.Message>};
  static serializeBinaryToWriter(message: Subsystem, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Subsystem;
  static deserializeBinaryFromReader(message: Subsystem, reader: jspb.BinaryReader): Subsystem;
}

export namespace Subsystem {
  export type AsObject = {
    name: string,
    startuptype: string,
    uitype: string,
    path: string,
    url: string,
    argumentsList: Array<string>,
    port: number,
    state: string,
    description: string,
    automatedstart: boolean,
  }
}

export interface ActionTypeMap {
  ADDPROCESSLISTACTION: 0;
  ADDPROCESSACTION: 1;
  REMOVEPROCESSBYIDACTION: 2;
  UPDATEPROCESSACTION: 3;
  UPDATEPROCESSSTATUSACTION: 4;
  ADDRUNTIMEINFOACTION: 5;
  ADDCONNECTIONLISTACTION: 6;
  UPDATECONNECTIONACTION: 7;
  UPDATECONNECTIONSTATUSACTION: 8;
  UPDATEENVIRONMENTVARIABLESACTION: 9;
  UPDATEMODULESACTION: 10;
  UPDATEREGISTRATIONSACTION: 11;
  UPDATESUBSYSTEMACTION: 12;
  INITSUBSYSTEMSACTION: 13;
  MODIFYSUBSYSTEMACTION: 14;
  REMOVESUBSYSTEMSACTION: 15;
  ADDSUBSYSTEMACTION: 16;
  ADDSUBSYSTEMSACTION: 17;
  RESTARTSUBSYSTEMSACTION: 18;
  TERMINATESUBSYSTEMSACTION: 19;
  LAUNCHSUBSYSTEMSACTION: 20;
  LAUNCHSUBSYSTEMSWITHDELAYACTION: 21;
  SUBSCRIPTIONALIVEACTION: 22;
}

export const ActionType: ActionTypeMap;

