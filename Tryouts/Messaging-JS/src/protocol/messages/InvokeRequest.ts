import { MessageBuffer } from "../../MessageBuffer";
import { MessageScope } from "../../MessageScope";
import { AbstractRequest } from "./AbstractRequest";
import { InvokeResponse } from "./InvokeResponse";
import { Message } from "./Message";

export interface InvokeRequest extends AbstractRequest<InvokeResponse> {
    type: "Invoke";
    endpoint: string;
    scope?: MessageScope;
    payload?: MessageBuffer;
    sourceId?: string;
    correlationId?: string;
}

export function isInvokeRequest(message: Message): message is InvokeRequest {
    return message.type == "Invoke";
}
