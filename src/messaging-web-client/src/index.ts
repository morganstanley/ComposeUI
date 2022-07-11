enum ClientState {
    Created = 'crated',
    WsConnecting = 'ws-connecting',
    ClientConnecting = 'client-connecting',
    Connected = 'connected',
    Closed = 'closed'
}

type PromiseCallback = {
    success: (data?: any) => void | Promise<any>;
    fail?: (data?: any) => void;
    onetime?: boolean;
};

export class ComposeMessagingClient {
    private clientId?: string;
    private lastRequestId = 0;

    private websocket?: WebSocket;
    private asyncCallbacks = new Map<string, PromiseCallback[]>();

    private state = ClientState.Created;

    constructor(private websocketUrl: string) {
    }

    public connect() {
        return new Promise((resolve, reject) => {
            this.state = ClientState.WsConnecting;

            this.websocket = new WebSocket(this.websocketUrl);
            this.websocket.addEventListener('message', this.handleMessage.bind(this));
            this.websocket.addEventListener('error', this.handleError.bind(this));
            this.websocket.addEventListener('open', () => {
                this.state = ClientState.ClientConnecting;
                this.sendMsg({ type: 'Connect' });
            });
            this.websocket.addEventListener('close', () => {
                this.state = ClientState.Closed;
                this.websocket = undefined;
            });
            
            this.addCallback('connect', resolve, reject, true);
        });
    }

    public subscribe(topic: string, handler: (message: any) => void) {
        if (this.addCallback('topic-' + topic, handler)) {
            this.sendMsg({ type: 'Subscribe', topic });
        }
    }

    public unsubscribe(topic: string, handler: (message: any) => void) {
        if (this.removeCallback('topic-' + topic, handler)) {
            this.sendMsg({ type: 'Unsubscribe', topic });
        }
    }

    public publish(topic: string, payload: any) {
        this.sendMsg({ type: 'Publish', topic, payload: JSON.stringify(payload) });
    }

    public invoke(serviceName: string, payload: any) {
        return new Promise((resolve, reject) => {
            const requestId = '' + (++this.lastRequestId);
            this.addCallback('request-' + requestId, resolve, reject, true);
            this.sendMsg({ type: 'Invoke', serviceName, payload: JSON.stringify(payload), requestId });
        });
    }

    public registerService(serviceName: string, handler: (payload: any) => any) {
        return new Promise((resolve, reject) => {
            this.addCallback('invoke-' + serviceName, handler);
            this.addCallback('register-' + serviceName, resolve, reject, true);
            this.sendMsg({ type: 'RegisterService', serviceName });
        });
    }

    private sendMsg(message: any) {
        if (this.websocket) {
            this.websocket?.send(JSON.stringify(message));
        } else {
            console.error('Websocket is not connected, call .connect() first');
        }
    }

    private handleMessage(event: MessageEvent) {
        const message = JSON.parse(event.data);
        switch (message.type) {
            case 'ConnectResponse': {
                this.clientId = message.clientId;
                this.state = ClientState.Connected;
                this.asyncDone('connect', 'success');
                break;
            }
            case 'Update': {
                this.asyncDone('topic-' + message.topic, 'success', JSON.parse(message.payload));
                break;
            }
            case 'InvokeResponse': {
                const isError = !!message.error;
                this.asyncDone('request-' + message.requestId, isError ? 'fail' : 'success', isError ? message.error : JSON.parse(message.payload));
                break;
            }
            case 'RegisterServiceResponse': {
                this.asyncDone('register-' + message.serviceName, message.error ? 'fail' : 'success', message.error);
                break;
            }
            case 'Invoke': {
                this.asyncDone('invoke-' + message.serviceName, 'success', JSON.parse(message.payload),
                    (response) => this.sendMsg({ type: 'InvokeResponse', requestId: message.requestId, payload: JSON.stringify(response) }),
                    (error) => this.sendMsg({ type: 'InvokeResponse', requestId: message.requestId, error: error?.toString() })
                );
                break;
            }
            default:
                throw new Error(`Invalid message type "${message.type}".`);
        }
    }

    private handleError(event: Event) {
        switch (this.state) {
            case ClientState.WsConnecting:
                this.asyncDone('connect', 'fail', 'Websocket error during connection.');
                break;
        }
    }

    private asyncDone(key: string, success: 'success'|'fail', value?: any, successFn?: (response: any) => void, failFn?: (error: any) => void) {
        const callbacks = this.asyncCallbacks.get(key);
        if (callbacks) {
            for (let i = 0; i < callbacks.length; i++) {
                const callback = callbacks[i];
                let response;
                try {
                    response = callback[success]?.(value);
                } catch (ex: any) {
                    response = Promise.reject(ex);
                }
                
                const responsePromise = Promise.resolve(response);
                if (successFn) responsePromise.then(successFn);
                if (failFn) responsePromise.catch(failFn);

                if (callback.onetime) callbacks.splice(i--, 1);
            }
            if (callbacks.length === 0) this.asyncCallbacks.delete(key);
        } else {
            console.warn(`No async callback found "${key}".`);
        }
    }

    /**
     * @returns true if this was the first callback for the given `key`
     */
    private addCallback(key: string, success: (data?: any) => void | Promise<any>, fail?: (data?: any) => void, onetime = false) {
        let isFirstCallback = false;
        let callbacks = this.asyncCallbacks.get(key);
        if (!callbacks) {
            this.asyncCallbacks.set(key, callbacks = []);
            isFirstCallback = true;
        }
        callbacks.push({ success, fail, onetime });
        return isFirstCallback;
    }

    /**
     * @returns true if this was the last callback removed for the given `key`
     */
    private removeCallback(key: string, success: (data?: any) => void, fail?: (data?: any) => void) {
        const callbacks = this.asyncCallbacks.get(key);
        if (!callbacks) return false;

        const idx = callbacks.findIndex(callback => callback.success === success && (!fail || callback.fail === fail));
        if (idx < 0) return false;

        callbacks.splice(idx, 1);
        const isLastCallback = callbacks.length === 0;
        
        if (isLastCallback) this.asyncCallbacks.delete(key);

        return isLastCallback;
    }

}