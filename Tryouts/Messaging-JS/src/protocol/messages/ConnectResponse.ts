import { Message } from ".";
import { Error } from "..";

export interface ConnectResponse extends Message {
    type: "ConnectResponse";
    clientId?: string;
    error?: Error;
}

export function isConnectResponse(message: Message): message is ConnectResponse {
    return message.type == "ConnectResponse";
}