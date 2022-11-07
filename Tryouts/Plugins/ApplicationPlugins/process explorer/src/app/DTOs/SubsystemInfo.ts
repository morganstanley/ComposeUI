import { SubsystemState } from "./SubsystemState";

export class SubsystemInfo{
    Name: string;
    StartUpType: string;
    UIType: string;
    Path: string;
    Url: string;
    Arguments: string[];
    Port: number;
    EntryPoint: string;
    State: SubsystemState;
    Description: string;
    AutomatedStart: boolean;
}