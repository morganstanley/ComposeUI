import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
export declare class AsyncDisposableWrapper implements AsyncDisposable {
    private readonly messageRouterClient;
    private readonly serviceName;
    constructor(messageRouterClient: MessageRouter, serviceName: string);
    [Symbol.asyncDispose](): PromiseLike<void>;
}
