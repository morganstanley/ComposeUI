import { Message } from "./Message";
import { Error } from "../Error";

export interface AbstractResponse extends Message {
    requestId: string;
    error?: Error;
}

export function isResponse(message: Message): message is AbstractResponse {
    return (message.type === "InvokeResponse" 
            || message.type === "RegisterServiceResponse"
            || message.type === "UnregisterServiceResponse");
}
