import { Message } from ".";

export interface AbstractRequest<TResponse> extends Message {
    readonly requestId: string;
}
