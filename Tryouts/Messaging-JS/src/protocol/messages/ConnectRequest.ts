import { Message } from "./Message";

export interface ConnectRequest extends Message {
    type: "Connect";
    accessToken?: string;
}

export function isConnectRequest(message: Message): message is ConnectRequest {
    return message.type == "Connect";
}
