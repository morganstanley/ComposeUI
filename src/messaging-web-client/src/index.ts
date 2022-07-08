enum ClientState {
    Created = 'crated',
    WsConnecting = 'ws-connecting',
    ClientConnecting = 'client-connecting',
    Connected = 'connected'
}

type PromiseCallbacks = {
    resolve: (data?: any) => void;
    reject?: (data?: any) => void;
    onetime?: boolean;
};

export class ComposeMessagingClient {
    private clientId?: string;

    constructor(private websocketUrl: string) {
    }

    private websocket?: WebSocket;
    private asyncCallbacks = new Map<string, PromiseCallbacks>();

    private state = ClientState.Created;

    public connect() {
        return new Promise((resolve, reject) => {
            this.state = ClientState.WsConnecting;

            this.websocket = new WebSocket(this.websocketUrl);
            this.websocket.addEventListener('message', this.handleMessage.bind(this));
            this.websocket.addEventListener('error', this.handleError.bind(this));
            this.websocket.addEventListener('open', () => {
                this.state = ClientState.ClientConnecting;
                this.sendMsg({ type:'Connect' });
            });
            
            this.asyncCallbacks.set('connect', { resolve, reject, onetime: true });
        });
    }

    public subscribe(topic: string, handler: (message: any) => void) {
        // TODO: multiple subscribers?
        this.asyncCallbacks.set(topic, { resolve: handler })
        this.sendMsg({ type: 'Subscribe', topic });
    }

    public publish(topic: string, payload: any) {
        this.sendMsg({ type: 'Publish', topic, payload: JSON.stringify(payload) });
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
            case 'ConnectResponse':
                this.clientId = message.clientId;
                this.state = ClientState.Connected;
                this.asyncDone('connect', 'resolve');
                break;
            case 'Update':
                this.asyncDone(message.topic, 'resolve', JSON.parse(message.payload));
                break;
        }
    }

    private handleError(event: Event) {
        switch (this.state) {
            case ClientState.WsConnecting:
                this.asyncDone('connect', 'reject', 'Websocket error during connection.');
                break;
        }
    }

    private asyncDone(key: string, success: 'resolve'|'reject', value?: any) {
        const callbacks = this.asyncCallbacks.get(key);
        if (callbacks) {
            callbacks[success]?.(value);
            if (callbacks.onetime) this.asyncCallbacks.delete(key);
        } else {
            console.warn(`No async callback found "${key}".`);
        }
    }

}