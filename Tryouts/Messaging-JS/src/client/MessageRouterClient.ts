import { Unsubscribable } from "rxjs";
import { EndpointDescriptor } from "../EndpointDescriptor";
import { createProtocolError, DuplicateEndpointError, MessageRouterError, ThrowHelper, UnknownEndpointError } from "../exceptions";
import { InvokeOptions } from "../InvokeOptions";
import { MessageBuffer } from "../MessageBuffer";
import { MessageHandler } from "../MessageHandler";
import { MessageRouter } from "../MessageRouter";
import { MessageScope } from "../MessageScope";
import * as messages from "../protocol/messages";
import { PublishOptions } from "../PublishOptions";
import { TopicMessage } from "../TopicMessage";
import { TopicSubscriber } from "../TopicSubscriber";
import { Connection } from "./Connection";
import { Deferred } from "./Deferred";
import { MessageRouterOptions } from "./MessageRouterOptions";

export enum ClientState {
    Created,
    Connecting,
    Connected,
    Closing,
    Closed
}

export class MessageRouterClient implements MessageRouter {

    constructor(private connection: Connection, private options: MessageRouterOptions) {
        this.options = options ?? {};
    }

    clientId?: string;

    connect(): Promise<void> {
        switch (this._state) {
            case ClientState.Connected:
                return Promise.resolve();
            case ClientState.Created:
                return this.connectCore();
            case ClientState.Connecting:
                return this.connected.promise;
        }

        throw ThrowHelper.connectionClosed();
    }

    close(): Promise<void> {
        switch (this._state) {
            case ClientState.Created:
                {
                    this._state = ClientState.Closed;
                    return Promise.resolve();
                }
            case ClientState.Closed:
                return Promise.resolve();
            case ClientState.Closing:
                return this.closed.promise;
        }

        return this.closeCore();
    }

    async subscribe(topicName: string, subscriber: TopicSubscriber | ((message: TopicMessage) => void)): Promise<Unsubscribable> {
        this.checkState();
        let needsSubscription = false;
        let topic = this.topics[topicName];

        if (!topic) {
            this.topics[topicName] = topic = new Topic(() => this.unsubscribe(topicName));
            needsSubscription = true;
        }

        if (typeof subscriber === "function") {
            subscriber = { next: subscriber };
        }

        const subscription = topic.subscribe(subscriber);

        if (needsSubscription) {
            await this.sendMessage<messages.SubscribeMessage>(
                {
                    type: "Subscribe",
                    topic: topicName
                });
        }

        return subscription;
    }

    publish(topic: string, payload?: MessageBuffer, options?: PublishOptions): Promise<void> {
        this.checkState();

        return this.sendMessage<messages.PublishMessage>(
            {
                type: "Publish",
                topic,
                payload,
                scope: options?.scope,
                correlationId: options?.correlationId
            });
    }

    async invoke(endpoint: string, payload?: MessageBuffer, options?: InvokeOptions): Promise<MessageBuffer | undefined> {
        this.checkState();

        const response = await this.sendRequest<messages.InvokeRequest, messages.InvokeResponse>(
            {
                type: "Invoke",
                requestId: this.getRequestId(),
                endpoint,
                payload,
                scope: options?.scope,
                correlationId: options?.correlationId
            });

        return response.payload;
    }

    async registerService(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor | undefined): Promise<void> {
        this.checkState();

        if (this.endpointHandlers[endpoint])
            throw new DuplicateEndpointError({ endpoint });

        this.endpointHandlers[endpoint] = handler;

        try {
            await this.sendRequest<messages.RegisterServiceRequest, messages.RegisterServiceResponse>(
                {
                    type: "RegisterService",
                    requestId: this.getRequestId(),
                    endpoint,
                    descriptor
                }
            );
        } catch (error) {
            delete this.endpointHandlers[endpoint];
            throw error;
        }
    }

    async unregisterService(endpoint: string): Promise<void> {
        this.checkState();

        if (!this.endpointHandlers[endpoint])
            return;

        await this.sendRequest<messages.UnregisterServiceRequest, messages.UnregisterServiceResponse>(
            {
                type: "UnregisterService",
                requestId: this.getRequestId(),
                endpoint
            }
        );

        delete this.endpointHandlers[endpoint];
    }

    registerEndpoint(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor | undefined): Promise<void> {
        this.checkState();

        if (this.endpointHandlers[endpoint])
            throw new DuplicateEndpointError({ endpoint });

        this.endpointHandlers[endpoint] = handler;

        return Promise.resolve();
    }

    unregisterEndpoint(endpoint: string): Promise<void> {
        this.checkState();

        delete this.endpointHandlers[endpoint];

        return Promise.resolve();
    }

    get state(): ClientState {
        return this._state;
    }

    private _state = ClientState.Created;
    private lastRequestId = 0;
    private topics: Record<string, Topic> = {};
    private connected = new Deferred<void>();
    private closed = new Deferred<void>();
    private pendingRequests: Record<string, Deferred<messages.AbstractResponse>> = {};
    private endpointHandlers: Record<string, MessageHandler> = {};

    private async connectCore(): Promise<void> {
        this._state = ClientState.Connecting;
        this.connection.onMessage((msg) => this.handleMessage(msg));
        this.connection.onError(err => this.handleError(err));
        this.connection.onClose(() => this.handleClose());
        await this.connection.connect();

        const req: messages.ConnectRequest = {
            type: "Connect",
            accessToken: this.options.accessToken
        };

        await this.connection.send(req);
        await this.connected.promise;
    }

    private async closeCore(): Promise<void> {
        this._state = ClientState.Closing;
        await this.connection.close();
        const err = ThrowHelper.connectionClosed();
        this.failPendingRequests(err);
        this.failSubscribers(err);
        this._state = ClientState.Closed;
        this.closed.resolve();
    }

    private failPendingRequests(err: any) {
        for (let requestId in this.pendingRequests) {
            this.pendingRequests[requestId].reject(err);
            delete this.pendingRequests[requestId];
        }
    }

    private async failSubscribers(err: any) {
        for (let topicName in this.topics) {
            const topic = this.topics[topicName];
            topic.error(err);
        }
    }

    private async sendMessage<TMessage extends messages.Message>(message: TMessage): Promise<void> {
        await this.connect();
        await this.connection.send(message);
    }

    private async sendRequest<TRequest extends messages.AbstractRequest<TResponse>, TResponse extends messages.AbstractResponse>(
        request: TRequest): Promise<TResponse> {

        await this.connect();
        const deferred = this.pendingRequests[request.requestId] = new Deferred<messages.AbstractResponse>();
        await this.sendMessage(request);
        return <TResponse>await deferred.promise;
    }

    private handleMessage(message: messages.Message): void {
        if (messages.isTopicMessage(message)) {
            this.handleTopicMessage(message);
            return;
        }

        if (messages.isResponse(message)) {
            this.handleResponse(message);
            return;
        }

        if (messages.isInvokeRequest(message)) {
            this.handleInvokeRequest(message);
            return;
        }

        if (messages.isConnectResponse(message)) {
            this.handleConnectResponse(message);
            return;
        }
    }

    private handleTopicMessage(message: messages.TopicMessage): void {
        const topic = this.topics[message.topic];

        if (!topic)
            return;

        topic.next({
            topic: message.topic,
            payload: message.payload,
            context: {
                sourceId: message.sourceId,
                scope: message.scope ?? MessageScope.default,
                correlationId: message.correlationId
            }
        });
    }

    private handleResponse(message: messages.AbstractResponse): void {
        const request = this.pendingRequests[message.requestId];

        if (!request)
            return;

        if (message.error) {
            request.reject(MessageRouterError.fromProtocolError(message.error));
        }
        else {
            request.resolve(message);
        }
    }

    private async handleInvokeRequest(message: messages.InvokeRequest): Promise<void> {

        try {

            const handler = this.endpointHandlers[message.endpoint];

            if (!handler)
                throw new UnknownEndpointError({ endpoint: message.endpoint });

            const result = await handler(
                message.endpoint,
                message.payload,
                {
                    scope: message.scope ?? MessageScope.default,
                    sourceId: message.sourceId!,
                    correlationId: message.correlationId
                });

            await this.sendMessage<messages.InvokeResponse>({
                type: "InvokeResponse",
                requestId: message.requestId,
                payload: typeof result === "string" ? result : undefined
            });

        } catch (error) {

            await this.sendMessage<messages.InvokeResponse>({
                type: "InvokeResponse",
                requestId: message.requestId,
                error: createProtocolError(error)
            });
        }
    }

    private handleConnectResponse(message: messages.ConnectResponse): void {
        if (message.error) {
            this._state = ClientState.Closed;
            this.connected.reject(new MessageRouterError(message.error));
        }
        else {
            this._state = ClientState.Connected;
            this.clientId = message.clientId;
            this.connected.resolve();
        }
    }

    private checkState() {
        if (this._state == ClientState.Closed || this._state == ClientState.Closing) {
            throw ThrowHelper.connectionClosed();
        }
    }

    private async handleError(err: any): Promise<void> {
        this.failPendingRequests(err);
        this.failSubscribers(err);
        
        if (this._state == ClientState.Connecting) {
            this.connected.reject(err);
            return;
        }

        this._state = ClientState.Closed;
        await this.connection?.close();
    }

    private handleClose(): Promise<void> {
        switch (this._state) {
            case ClientState.Closing:
            case ClientState.Closed:
                return Promise.resolve();
        }

        return this.handleError(ThrowHelper.connectionClosed());
    }

    private async unsubscribe(topicName: string): Promise<void> {
        let topic = this.topics[topicName];

        if (!topic)
            return;

        await this.sendMessage<messages.UnsubscribeMessage>(
            {
                type: "Unsubscribe",
                topic: topicName
            }
        );
    }

    private getRequestId(): string {
        return '' + (++this.lastRequestId);
    }
}

class Topic {

    constructor(onUnsubscribe: () => void) {
        this.onUnsubscribe = onUnsubscribe;
    }

    subscribe(subscriber: TopicSubscriber): Unsubscribable {
        this.subscribers.push(subscriber);

        return {
            unsubscribe: () => this.unsubscribe(subscriber)
        };
    }

    unsubscribe(subscriber: TopicSubscriber): void {
        const idx = this.subscribers.lastIndexOf(subscriber);

        if (idx < 0)
            return;

        this.subscribers.splice(idx, 1);

        if (this.subscribers.length == 0) {
            this.onUnsubscribe();
        }
    }

    next(message: TopicMessage): void {
        for (let subscriber of this.subscribers) {
            try {
                subscriber.next?.call(subscriber, message);
            }
            catch (err) {
                console.error(err);
            }
        }
    }

    error(error: any): void {
        for (let subscriber of this.subscribers) {
            try {
                subscriber.error?.call(subscriber, error);
            } catch (e) {
                console.error(e);
            }
        }
    }

    complete(): void {
        for (let subscriber of this.subscribers) {
            try {
                subscriber.complete?.call(subscriber);
            } catch (e) {
                console.error(e);
            }
        }
    }

    private onUnsubscribe: () => void;
    private subscribers: TopicSubscriber[] = [];
}
