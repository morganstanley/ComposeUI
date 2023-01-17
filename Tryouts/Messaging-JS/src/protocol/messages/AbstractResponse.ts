import { Message } from ".";
import { Error } from "..";

export interface AbstractResponse extends Message {
    requestId: string;
    error?: Error;
}

export function isResponse(message: Message): message is AbstractResponse {
    return (message.type === "InvokeResponse" 
            || message.type === "RegisterServiceResponse"
            || message.type === "UnregisterServiceResponse");
}
