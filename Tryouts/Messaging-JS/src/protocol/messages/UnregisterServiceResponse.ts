import { Message, AbstractResponse } from ".";

export interface UnregisterServiceResponse extends AbstractResponse {
    type: "UnregisterServiceResponse";
}

export function isUnregisterServiceResponse(message: Message): message is UnregisterServiceResponse {
    return message.type == "UnregisterService";
}
