export interface ISubsystemController{
    LaunchSubsystem(subsystemId: string): void;
    LaunchSubsystems(subsystemIds: Array<string>): void;
    RestartSubsystem(subsystemId: string): void;
    RestartSubsystems(subsystemIds: Array<string>): void;
    ShutdownSubsystem(subsystemId: string): void;
    ShutdownSubsystems(subsystemIds: Array<string>): void;
}