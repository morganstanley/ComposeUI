import { Message, AbstractResponse } from ".";

export interface RegisterServiceResponse extends AbstractResponse {
    type: "RegisterServiceResponse";
}

export function isRegisterServiceResponse(message: Message): message is RegisterServiceResponse {
    return message.type == "RegisterServiceResponse";
}
