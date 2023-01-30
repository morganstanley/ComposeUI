import * as messages from "../protocol/messages";

export type OnMessageCallback = (message: messages.Message) => void;
export type OnErrorCallback = (err: any) => void;
export type OnCloseCallback = () => void;

export interface Connection {
    connect(): Promise<void>;
    send(message: messages.Message): Promise<void>;
    close(): Promise<void>;
    onMessage(callback: OnMessageCallback): void;
    onError(callback: OnErrorCallback): void;
    onClose(callback: OnCloseCallback): void;
}
