import * as messages from "../../protocol/messages";
import { WebSocketOptions } from "./WebSocketOptions";
import { MessageScope } from "../../MessageScope";
import { Connection, OnMessageCallback, OnErrorCallback, OnCloseCallback } from "../Connection";

export class WebSocketConnection implements Connection {

    constructor(private options: WebSocketOptions) {
    }

    connect(): Promise<void> {
        
        return new Promise((resolve, reject) => {

            this.websocket = new WebSocket(this.options.url);

            this.websocket.addEventListener('open', () => {
                this.isConnected = true;
                resolve();
            });

            this.websocket.addEventListener('message', ev => {
                const message = WebSocketConnection.deserializeMessage(ev.data);
                this._onMessage?.call(undefined, message);
            });
            
            this.websocket.addEventListener('error', ev => {
                if (!this.isConnected) {
                    reject();
                }
                else {
                    this.isConnected = false;
                    this._onError?.call(undefined, new Error());
                }
            });

            this.websocket.addEventListener('close', () => {
                this.isConnected = false;
                delete this.websocket;
                this._onClose?.call(undefined);
            });
        });
    }
    
    send(message: messages.Message): Promise<void> {
        if (!this.websocket) return Promise.reject();
        this.websocket.send(JSON.stringify(message));
        return Promise.resolve();
    }

    close(): Promise<void> {
        if (this.isConnected) {
            this.websocket?.close(1000, "Closed by client");
            this.isConnected = false;
            delete this.websocket;
        }
        return Promise.resolve();
    }

    onMessage(callback: OnMessageCallback): void {
        this._onMessage = callback;
    }

    onError(callback: OnErrorCallback): void {
        this._onError = callback;
    }

    onClose(callback: OnCloseCallback): void {
        this._onClose = callback;
    }

    private static deserializeMessage(data: any): messages.Message {
        const msg = <messages.Message> JSON.parse(data);
        if ("scope" in msg && typeof(msg.scope) === "string") {
            msg.scope = MessageScope.parse(msg.scope);
        }

        return msg;
    }

    private websocket?: WebSocket;
    private isConnected: boolean = false;
    private messageQueue: messages.Message[] = [];
    private _onMessage?: OnMessageCallback;
    private _onError?: OnErrorCallback;
    private _onClose?: OnCloseCallback;

}