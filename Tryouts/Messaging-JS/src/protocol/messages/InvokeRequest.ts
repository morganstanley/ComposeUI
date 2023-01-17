import { Message, InvokeResponse, AbstractRequest } from ".";
import { MessageBuffer, MessageScope } from "../..";

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
