import { EndpointDescriptor } from "../..";
import { AbstractRequest } from "./AbstractRequest";
import { Message } from "./Message";
import { RegisterServiceResponse } from "./RegisterServiceResponse";

export interface RegisterServiceRequest extends AbstractRequest<RegisterServiceResponse> {
    type: "RegisterService";
    endpoint: string;
    descriptor?: EndpointDescriptor;
}

export function isRegisterServiceRequest(message: Message): message is RegisterServiceRequest {
    return message.type == "RegisterService";
}
