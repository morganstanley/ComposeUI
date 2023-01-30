import { Message } from "./Message";

export interface AbstractRequest<TResponse> extends Message {
    readonly requestId: string;
}
