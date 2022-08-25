import { CallbackStore } from './CallbackStore';

enum ClientState {
    Created = 'crated',
    WsConnecting = 'ws-connecting',
    ClientConnecting = 'client-connecting',
    Connected = 'connected',
    Closed = 'closed'
}

export class ComposeMessagingClient {
    private clientId?: string;
    private lastRequestId = 0;

    private websocket?: WebSocket;
    private asyncCallbacks = new CallbackStore();

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
            
            this.asyncCallbacks.add('connect', resolve, reject, true);
        });
    }

    public subscribe(topic: string, handler: (message: any) => void) {
        if (this.asyncCallbacks.add('topic-' + topic, handler)) {
            this.sendMsg({ type: 'Subscribe', topic });
        }
    }

    public unsubscribe(topic: string, handler: (message: any) => void) {
        if (this.asyncCallbacks.remove('topic-' + topic, handler)) {
            this.sendMsg({ type: 'Unsubscribe', topic });
        }
    }

    public publish(topic: string, payload: any) {
        this.sendMsg({ type: 'Publish', topic, payload: JSON.stringify(payload) });
    }

    public invoke(serviceName: string, payload: any) {
        return new Promise((resolve, reject) => {
            const requestId = '' + (++this.lastRequestId);
            this.asyncCallbacks.add('request-' + requestId, resolve, reject, true);
            this.sendMsg({ type: 'Invoke', serviceName, payload: JSON.stringify(payload), requestId });
        });
    }

    public registerService(serviceName: string, handler: (payload: any) => any) {
        return new Promise((resolve, reject) => {
            this.asyncCallbacks.add('invoke-' + serviceName, handler);
            this.asyncCallbacks.add('register-' + serviceName, resolve, reject, true);
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
                this.asyncCallbacks.invoke('connect', 'success');
                break;
            }
            case 'Update': {
                this.asyncCallbacks.invoke('topic-' + message.topic, 'success', JSON.parse(message.payload));
                break;
            }
            case 'InvokeResponse': {
                const isError = !!message.error;
                this.asyncCallbacks.invoke('request-' + message.requestId, isError ? 'fail' : 'success', isError ? message.error : JSON.parse(message.payload));
                break;
            }
            case 'RegisterServiceResponse': {
                this.asyncCallbacks.invoke('register-' + message.serviceName, message.error ? 'fail' : 'success', message.error);
                break;
            }
            case 'Invoke': {
                this.asyncCallbacks.invoke('invoke-' + message.serviceName, 'success', JSON.parse(message.payload),
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
                this.asyncCallbacks.invoke('connect', 'fail', 'Websocket error during connection.');
                break;
        }
    }

}