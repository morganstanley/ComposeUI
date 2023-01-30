import { AbstractResponse } from "./AbstractResponse";
import { Message } from "./Message";

export interface UnregisterServiceResponse extends AbstractResponse {
    type: "UnregisterServiceResponse";
}

export function isUnregisterServiceResponse(message: Message): message is UnregisterServiceResponse {
    return message.type == "UnregisterService";
}
