import { AbstractRequest, Message, UnregisterServiceResponse } from ".";

export interface UnregisterServiceRequest extends AbstractRequest<UnregisterServiceResponse> {
    type: "UnregisterService";
    endpoint: string;
}

export function isUnregisterServiceRequest(message: Message): message is UnregisterServiceRequest {
    return message.type == "UnregisterService";
}
