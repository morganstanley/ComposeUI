import { MessageBuffer } from "../../MessageBuffer";
import { AbstractResponse } from "./AbstractResponse";
import { Message } from "./Message";

export interface InvokeResponse extends AbstractResponse {
    type: "InvokeResponse";
    payload?: MessageBuffer;
}

export function isInvokeResponse(message: Message): message is InvokeResponse {
    return message.type == "InvokeResponse";
}
