import { AbstractRequest, Message, RegisterServiceResponse } from ".";
import { EndpointDescriptor } from "../..";

export interface RegisterServiceRequest extends AbstractRequest<RegisterServiceResponse> {
    type: "RegisterService";
    endpoint: string;
    descriptor?: EndpointDescriptor;
}

export function isRegisterServiceRequest(message: Message): message is RegisterServiceRequest {
    return message.type == "RegisterService";
}
