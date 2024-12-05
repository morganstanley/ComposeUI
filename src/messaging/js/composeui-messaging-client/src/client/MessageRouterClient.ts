/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 *  
 */

import { PartialObserver, Unsubscribable } from "rxjs";
import { EndpointDescriptor } from "../EndpointDescriptor";
import { ErrorNames } from "../ErrorNames";
import { ThrowHelper } from "../exceptions";
import { InvokeOptions } from "../InvokeOptions";
import { MessageBuffer } from "../MessageBuffer";
import { MessageHandler } from "../MessageHandler";
import { MessageRouter } from "../MessageRouter";
import { createProtocolError, MessageRouterError } from "../MessageRouterError";
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

    private _clientId?: string;

    get clientId() {
        return this._clientId;
    }

    connect(): Promise<void> {
        switch (this._state) {
            case ClientState.Connected:
                return Promise.resolve();
            case ClientState.Created:
                return this.connectCore();
            case ClientState.Connecting:
                return this.connected.promise;
        }

        return Promise.reject(ThrowHelper.connectionClosed());
    }

    close(): Promise<void> {
        return this.closeCore();
    }

    async subscribe(topicName: string, subscriber: TopicSubscriber): Promise<Unsubscribable> {
        this.checkState();

        if (this.pendingUnsubscriptions[topicName]) {
            await this.pendingUnsubscriptions[topicName];
        }

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
            try {
                await this.sendRequest<messages.SubscribeMessage, messages.SubscribeResponse>(
                {
                    requestId: this.getRequestId(),
                    type: "Subscribe",
                    topic: topicName
                });
            } catch (error) {
                console.error("Failed to subscribe to topic '${topicName}'", error);
                delete this.topics[topicName];
            }
        }

        return subscription;
    }

    async publish(topic: string, payload?: MessageBuffer, options?: PublishOptions): Promise<void> {
        this.checkState();

        await this.sendRequest<messages.PublishMessage, messages.PublishResponse>(
            {
                type: "Publish",
                requestId: this.getRequestId(),
                topic,
                payload,
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
                correlationId: options?.correlationId
            });

        return response.payload;
    }

    async registerService(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor | undefined): Promise<void> {
        this.checkState();

        if (this.endpointHandlers[endpoint])
            throw ThrowHelper.duplicateEndpoint(endpoint);

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
            throw ThrowHelper.duplicateEndpoint(endpoint);

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
    private pendingUnsubscriptions: Record<string, Promise<void> | undefined> = {};

    private async connectCore(): Promise<void> {
        this._state = ClientState.Connecting;

        try {
            this.connection.onMessage(this.handleMessage.bind(this));
            this.connection.onError(this.handleError.bind(this));
            this.connection.onClose(this.handleClose.bind(this));
            await this.connection.connect();

            const req: messages.ConnectRequest = {
                type: "Connect",
                accessToken: this.options.accessToken
            };

            await this.connection.send(req);
            // This must be the last statement before catch so that awaiting `connected` 
            // has the same effect as awaiting `connect()`. `close()` also rejects this promise.
            await this.connected.promise;
        } catch (error: any) {
            if (error instanceof MessageRouterError) {
                throw error;
            }
            else {
                await this.closeCore(error);
                throw ThrowHelper.connectionFailed(error.message || error);
            }
        }
    }

    private async closeCore(error?: any): Promise<void> {
        error ??= ThrowHelper.connectionClosed();
        switch (this._state) {
            case ClientState.Created:
                {
                    this._state = ClientState.Closed;
                    return;
                }
            case ClientState.Closed:
                return;
            case ClientState.Closing:
                await this.closed.promise;
                return;
            case ClientState.Connecting:
                {
                    this._state = ClientState.Closed;
                    this.connected.reject(ThrowHelper.connectionClosed());
                    return;
                }
        }
        this._state = ClientState.Closing;
        this.failPendingRequests(error);
        this.failSubscribers(error);
        try {
            await this.connection.close();
        }
        catch (e) {
            console.error(e)
        }
        this._state = ClientState.Closed;
        this.closed.resolve();
    }

    private failPendingRequests(error: any) {
        for (let requestId in this.pendingRequests) {
            this.pendingRequests[requestId].reject(error);
            delete this.pendingRequests[requestId];
        }
    }

    private async failSubscribers(error: any) {
        for (let topicName in this.topics) {
            const topic = this.topics[topicName];
            topic.error(error);
        }
    }

    private async sendMessage<TMessage extends messages.Message>(message: TMessage): Promise<void> {
        await this.connect();
        await this.connection.send(message);
    }

    private async sendRequest<TRequest extends messages.AbstractRequest<TResponse>, TResponse extends messages.AbstractResponse>(
        request: TRequest): Promise<TResponse> {

        const deferred = this.pendingRequests[request.requestId] = new Deferred<messages.AbstractResponse>();

        try {
            await this.sendMessage(request);
        } catch (error) {
            delete this.pendingRequests[request.requestId];
            throw error;
        }

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
                correlationId: message.correlationId
            }
        });
    }

    private handleResponse(message: messages.AbstractResponse): void {
        const request = this.pendingRequests[message.requestId];

        if (!request)
            return;

        if (message.error) {
            request.reject(new MessageRouterError(message.error));
        }
        else {
            request.resolve(message);
        }
    }

    private async handleInvokeRequest(message: messages.InvokeRequest): Promise<void> {

        try {

            const handler = this.endpointHandlers[message.endpoint];

            if (!handler)
                throw ThrowHelper.unknownEndpoint(message.endpoint);

            const result = await handler(
                message.endpoint,
                message.payload,
                {
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
            this._clientId = message.clientId;
            this._state = ClientState.Connected;
            this.connected.resolve();
        }
    }

    private checkState() {
        if (this._state == ClientState.Closed || this._state == ClientState.Closing) {
            throw ThrowHelper.connectionClosed();
        }
    }

    private handleError(error: any): void {
        switch (this._state) {
            case ClientState.Closing:
            case ClientState.Closed:
                return;
        }

        this.closeCore(error);
    }

    private handleClose(): void {
        this.handleError(ThrowHelper.connectionAborted());
    }

    private async unsubscribe(topicName: string): Promise<void> {
        let topic = this.topics[topicName];

        if (!topic)
            return;

        if (this.pendingUnsubscriptions[topicName]) {
            await this.pendingUnsubscriptions[topicName];
        }

        this.pendingUnsubscriptions[topicName] = this.sendRequest<messages.UnsubscribeMessage, messages.UnsubscribeResponse>({
            requestId: this.getRequestId(),
            type: "Unsubscribe",
            topic: topicName
        })
        .then(() => {
            delete this.topics[topicName];
        })
        .catch(error => {
            console.error("Exception thrown while unsubscribing.", error);
            throw error;
        })
        .finally(() => {
            delete this.pendingUnsubscriptions[topicName];
        });

        await this.pendingUnsubscriptions[topicName];
    }

    private getRequestId(): string {
        return '' + (++this.lastRequestId);
    }
}

type TopicSubscriberInternal = PartialObserver<TopicMessage>;

class Topic {

    constructor(onUnsubscribe: () => void) {
        this.onUnsubscribe = onUnsubscribe;
    }

    subscribe(subscriber: TopicSubscriberInternal): Unsubscribable {
        if (this.isCompleted) return {
            unsubscribe: () => { }
        };;

        this.subscribers.push(subscriber);

        return {
            unsubscribe: () => this.unsubscribe(subscriber)
        };
    }

    unsubscribe(subscriber: TopicSubscriberInternal): void {
        if (this.isCompleted) return;

        const idx = this.subscribers.lastIndexOf(subscriber);

        if (idx < 0)
            return;

        this.subscribers.splice(idx, 1);

        if (this.subscribers.length == 0) {
            this.onUnsubscribe();
        }
    }

    next(message: TopicMessage): void {
        if (this.isCompleted) {
            return;
        }

        for (let subscriber of this.subscribers) {
            try {
                subscriber.next?.call(subscriber, message);
            }
            catch (error) {
                console.error(error);
            }
        }
    }

    error(error: any): void {
        if (this.isCompleted) return;

        this.isCompleted = true;

        for (let subscriber of this.subscribers) {
            try {
                subscriber.error?.call(subscriber, error);
            } catch (e) {
                console.error(e);
            }
        }
    }

    complete(): void {
        if (this.isCompleted) return;

        for (let subscriber of this.subscribers) {
            try {
                subscriber.complete?.call(subscriber);
            } catch (e) {
                console.error(e);
            }
        }
    }

    private isCompleted: boolean = false;
    private onUnsubscribe: () => void;
    private subscribers: TopicSubscriberInternal[] = [];
}
