import { AbstractResponse, Message } from ".";
import { MessageBuffer } from "../..";

export interface InvokeResponse extends AbstractResponse {
    type: "InvokeResponse";
    payload?: MessageBuffer;
}

export function isInvokeResponse(message: Message): message is InvokeResponse {
    return message.type == "InvokeResponse";
}
