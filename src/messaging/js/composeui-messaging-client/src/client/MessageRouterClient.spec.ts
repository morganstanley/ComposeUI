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

import "jest-extended";
import { ClientState, MessageRouterClient } from "./MessageRouterClient";
import * as messages from "../protocol/messages";
import { Error } from "../protocol";
import { MessageContext } from "../MessageContext";
import { MessageHandler } from "../MessageHandler";
import { TopicMessage } from "../TopicMessage";
import { TopicSubscriber } from "../TopicSubscriber";
import { Connection, OnMessageCallback, OnErrorCallback, OnCloseCallback } from "./Connection";
import { ErrorNames } from "../ErrorNames";
import { MessageRouterError } from "../MessageRouterError";
import { describe } from "node:test";

describe("MessageRouterClient", () => {

    let connection: MockConnection;

    beforeEach(() => {

        connection = new MockConnection();

        connection.handle<messages.ConnectRequest>(
            "Connect",
            msg => connection.sendToClient<messages.ConnectResponse>({ type: "ConnectResponse", clientId: "client-id" })
        );
    });

    function it_throws_if_previously_closed(action: (client: MessageRouterClient) => Promise<void>) {

        it("throws if the client was previously closed", async () => {

            const client = new MessageRouterClient(connection, {});

            await client.close();

            await expect(action(client)).rejects.toThrowWithName(MessageRouterError, ErrorNames.connectionClosed);
        });
    }

    describe("connect", () => {

        it_throws_if_previously_closed(client => client.connect());

        it("sends a ConnectRequest and waits for a ConnectResponse", async () => {

            connection.send = jest.fn(() => Promise.resolve());
            const client = new MessageRouterClient(connection, {});

            const connectPromise = client.connect();
            await new Promise(process.nextTick);

            expect(client.state).toBe(ClientState.Connecting);
            expect(connection.mock.connect).toHaveBeenCalled();
            expect(connection.mock.send).toHaveBeenCalledWith({ type: "Connect" });
            expect(connection.mock.send).toHaveBeenCalledAfter(connection.mock.connect);
            expect(client.clientId).toBeUndefined();

            await connection.sendToClient<messages.ConnectResponse>({ type: "ConnectResponse", clientId: "connected-client-id" });
            await new Promise(process.nextTick);
            await connectPromise;

            expect(client.state).toBe(ClientState.Connected);
            expect(client.clientId).toBe("connected-client-id");
        });

        it("transitions to the Closed state and throws a MessageRouterError if the ConnectResponse contains an error", async () => {

            connection.handle<messages.ConnectRequest>(
                "Connect",
                req => connection.sendToClient<messages.ConnectResponse>({
                    type: "ConnectResponse",
                    error: {
                        name: "Error",
                        message: "Connect failed"
                    }
                }))

            const client = new MessageRouterClient(connection, {});

            try {
                await client.connect();
            } catch (error: any) {
                expect(error).toBeInstanceOf(MessageRouterError);
                expect(error.name).toBe("Error");
                expect(error.message).toBe("Connect failed");
            }
            expect(client.state).toBe(ClientState.Closed);

        });
    });

    describe("close", () => {

        it("when called before connecting, transitions to Closed state without invoking the connection object", async () => {

            const client = new MessageRouterClient(connection, {});

            await client.close();

            expect(client.state).toBe(ClientState.Closed);
            expect(connection.mock.connect).not.toHaveBeenCalled();
            expect(connection.mock.send).not.toHaveBeenCalled();
            expect(connection.mock.close).not.toHaveBeenCalled();
        });

        it("when called while connecting, transitions to Closed state", async () => {

            const client = new MessageRouterClient(connection, {});
            connection.handle<messages.ConnectRequest>("Connect", () => Promise.resolve());
            const connectPromise = client.connect();

            await client.close();

            expect(client.state).toBe(ClientState.Closed);
            expect(connection.mock.connect).toHaveBeenCalled();
            expect(connection.mock.close).not.toHaveBeenCalled();
            await expect(connectPromise).rejects.toThrowWithName(MessageRouterError, ErrorNames.connectionClosed);
        });

        it("closes the connection and transitions to Closed state", async () => {

            const client = new MessageRouterClient(connection, {});

            await client.connect();
            await client.close();

            expect(client.state).toBe(ClientState.Closed);
            expect(connection.mock.close).toHaveBeenCalled();

        });

        it("does not throw if the client is already closed", async () => {

            const client = new MessageRouterClient(connection, {});

            await client.close();
            await client.close();
        });

        it("calls error on active subscribers", async () => {

            const client = new MessageRouterClient(connection, {});
            const subscriber: TopicSubscriber = {
                error: jest.fn()
            };

            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", subscriber);

            await client.close();

            expect(subscriber.error).toHaveBeenCalledOnce();
        });

        it("completes pending requests with an error", async () => {

            const client = new MessageRouterClient(connection, {});

            const invokePromise = client.invoke("test-endpoint");

            await client.close();

            await expect(invokePromise).rejects.toThrowWithName(MessageRouterError, ErrorNames.connectionClosed);
        });


    });

    describe("publish", () => {

        it_throws_if_previously_closed(client => client.publish("test-topic", "test-payload"));

        it("sends a PublishMessage with the provided arguments", async () => {

            const client = new MessageRouterClient(connection, {});

            connection.handle<messages.PublishMessage>(
                "Publish",
                msg => connection.sendToClient<messages.PublishResponse>({ type: "PublishResponse", requestId: msg.requestId }));

            await client.publish("test-topic", "test-payload", { correlationId: "test-correlation-id" });

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.PublishMessage>{
                    type: "Publish",
                    topic: "test-topic",
                    payload: "test-payload",
                    correlationId: "test-correlation-id"
                })
            );
        });
    });

    describe("subscribe", () => {

        it_throws_if_previously_closed(
            async client => {
                await client.subscribe("test-topic", { next: () => { } });
            });

        it("sends a Subscribe message", async () => {

            const client = new MessageRouterClient(connection, {});
            
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", { next: () => { } });

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.SubscribeMessage>{
                    type: "Subscribe",
                    topic: "test-topic"
                }));
        });

    });

    describe("when receiving a Topic message", () => {

        it("invokes the subscribed Observer", async () => {

            const client = new MessageRouterClient(connection, {});

            const observer = {
                next: jest.fn()
            };

            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", observer);

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await new Promise(process.nextTick);

            expect(observer.next).toHaveBeenCalledWith(
                expect.objectContaining(<TopicMessage>{
                    topic: "test-topic",
                    payload: "test-payload",
                    context: {
                        sourceId: "test-source-id",
                        correlationId: "test-correlation-id"
                    }
                }));

        });

        it("invokes the subscribed callback", async () => {

            const client = new MessageRouterClient(connection, {});
            const subscriber = jest.fn();
            
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", subscriber);

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await new Promise(process.nextTick);

            expect(subscriber).toHaveBeenCalledWith(
                expect.objectContaining(<TopicMessage>{
                    topic: "test-topic",
                    payload: "test-payload",
                    context: {
                        sourceId: "test-source-id",
                        correlationId: "test-correlation-id"
                    }
                }));

        });

        it("keeps processing messages if the subscribed callback issues another invocation", async () => {

            const client = new MessageRouterClient(connection, {});
            const subscriber = <TopicSubscriber>jest.fn((msg: TopicMessage) => client.invoke("test-service", msg.payload));
            
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", subscriber);

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload-1",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload-2",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await new Promise(process.nextTick);

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeRequest>{
                    type: "Invoke",
                    endpoint: "test-service",
                    payload: "test-payload-1"
                }));

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeRequest>{
                    type: "Invoke",
                    endpoint: "test-service",
                    payload: "test-payload-2"
                }));

        });

        it("keeps processing messages if the subscribed Observer issues another invocation", async () => {

            const client = new MessageRouterClient(connection, {});

            const subscriber = <TopicSubscriber>{
                next: jest.fn((msg: TopicMessage) => client.invoke("test-service", msg.payload))
            };

            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", subscriber);

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload-1",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await connection.sendToClient<messages.TopicMessage>({
                type: "Topic",
                topic: "test-topic",
                payload: "test-payload-2",
                sourceId: "test-source-id",
                correlationId: "test-correlation-id"
            });

            await new Promise(process.nextTick);

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeRequest>{
                    type: "Invoke",
                    endpoint: "test-service",
                    payload: "test-payload-1"
                }));

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeRequest>{
                    type: "Invoke",
                    endpoint: "test-service",
                    payload: "test-payload-2"
                }));
        });
    });

    describe("invoke", () => {

        it_throws_if_previously_closed(async client => {
            await client.invoke("test-endpoint");
        });

        it("sends an InvokeRequest with the provided arguments and waits for the response", async () => {

            connection.handle<messages.InvokeRequest>(
                "Invoke",
                msg => connection.sendToClient<messages.InvokeResponse>({ type: "InvokeResponse", requestId: msg.requestId, payload: "Re: " + msg.payload })
            );

            const client = new MessageRouterClient(connection, {});

            let response = await client.invoke("test-endpoint", "test-request", { correlationId: "test-correlation-id" });

            expect(response).toBe("Re: test-request");

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeRequest>{
                    type: "Invoke",
                    endpoint: "test-endpoint",
                    payload: "test-request",
                    correlationId: "test-correlation-id",
                    requestId: expect.anything()
                }));
        });

        it("throws if the InvokeResponse contains an error", async () => {

            connection.handle<messages.InvokeRequest>(
                "Invoke",
                msg => connection.sendToClient<messages.InvokeResponse>({ type: "InvokeResponse", requestId: msg.requestId, error: { name: "Error", message: "Invoke failed" } })
            );

            const client = new MessageRouterClient(connection, {});

            await expect(client.invoke("test-endpoint", "test-request")).rejects.toThrow({ name: "Error", message: "Invoke failed" });
        });
    });

    describe("registerService", () => {

        it_throws_if_previously_closed(client => client.registerService("test-service", () => Promise.resolve()));

        it("sends a RegisterServiceRequest and waits for the response", async () => {

            const client = new MessageRouterClient(connection, {});
            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId }));

            await client.registerService(
                "test-service",
                () => Promise.resolve(),
                { description: "This is a test service" });

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.RegisterServiceRequest>{
                    type: "RegisterService",
                    endpoint: "test-service",
                    requestId: expect.anything()
                }));
        });

        it("throws DuplicateEndpoint if the endpoint is already registered", async () => {

            const client = new MessageRouterClient(connection, {});

            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId }));

            await client.registerService("test-service", () => { });

            await expect(client.registerService("test-service", () => { })).rejects.toThrowWithName(MessageRouterError, ErrorNames.duplicateEndpoint);
        });

        it("throws DuplicateEndpoint from the error response", async () => {

            const client = new MessageRouterClient(connection, {});
            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId, error: { name: ErrorNames.duplicateEndpoint }, }));

            await expect(client.registerService("test-service", () => { })).rejects.toThrowWithName(MessageRouterError, ErrorNames.duplicateEndpoint);
        });
    });

    describe("unregisterService", () => {

        it_throws_if_previously_closed(client => client.unregisterService("test-service"));

        it("sends an UnregisterServiceRequest and waits for the response", async () => {

            const client = new MessageRouterClient(connection, {});

            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId }));

            connection.handle<messages.UnregisterServiceRequest>(
                "UnregisterService",
                msg => connection.sendToClient<messages.UnregisterServiceResponse>({ type: "UnregisterServiceResponse", requestId: msg.requestId }));

            await client.registerService("test-service", () => Promise.resolve());
            await client.unregisterService("test-service");

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.UnregisterServiceRequest>{
                    type: "UnregisterService",
                    endpoint: "test-service",
                    requestId: expect.anything()
                })
            );

        });
    });

    describe("when responding to Invoke messages", () => {

        it("invokes the registered handler for a service and responds with InvokeResponse", async () => {

            const client = new MessageRouterClient(connection, {});

            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId }));

            const handler: MessageHandler = (endpoint, payload, context: MessageContext) => "Re: " + payload
            const mockHandler = jest.fn(handler);

            await client.registerService("test-service", mockHandler);

            await connection.sendToClient<messages.InvokeRequest>({
                type: "Invoke",
                endpoint: "test-service",
                requestId: "1",
                payload: "test-payload",
                sourceId: "sender-id",
                correlationId: "test-correlation-id"
            });

            await new Promise(process.nextTick);

            expect(mockHandler).toHaveBeenCalledWith(
                "test-service",
                "test-payload",
                expect.objectContaining(<MessageContext>{
                    sourceId: "sender-id",
                    correlationId: "test-correlation-id"
                })
            );

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeResponse>{
                    type: "InvokeResponse",
                    requestId: "1",
                    payload: "Re: test-payload"
                })
            );
        });

        it("sends an InvokeResponse with error 'Error' if the handler threw an exception", async () => {

            const client = new MessageRouterClient(connection, {});

            connection.handle<messages.RegisterServiceRequest>(
                "RegisterService",
                msg => connection.sendToClient<messages.RegisterServiceResponse>({ type: "RegisterServiceResponse", requestId: msg.requestId }));

            const mockHandler = jest.fn(() => {
                throw new Error("Epic fail")
            });

            await client.registerService("test-service", mockHandler);

            await connection.sendToClient<messages.InvokeRequest>({
                type: "Invoke",
                endpoint: "test-service",
                requestId: "1"
            });

            await new Promise(process.nextTick);

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeResponse>{
                    type: "InvokeResponse",
                    requestId: "1",
                    error: expect.objectContaining(<Error>{
                        name: "Error",
                        message: "Epic fail"
                    })
                })
            );
        });

        it("sends an InvokeResponse with error 'UnknownEndpoint' if the endpoint is not registered", async () => {

            const client = new MessageRouterClient(connection, {});

            await client.connect();

            await connection.sendToClient<messages.InvokeRequest>({
                type: "Invoke",
                endpoint: "unknown-endpoint",
                requestId: "1"
            });

            await new Promise(process.nextTick);

            expect(connection.mock.send).toHaveBeenCalledWith(
                expect.objectContaining(<messages.InvokeResponse>{
                    type: "InvokeResponse",
                    requestId: "1",
                    error: expect.objectContaining(<Error>{
                        name: ErrorNames.unknownEndpoint,
                    })
                })
            );
        });

        it("repeatedly calls the registered handler without waiting for it to complete asynchronously", async () => {

            const client = new MessageRouterClient(connection, {});
            const handler: MessageHandler = jest.fn((endpoint, payload, context) => new Promise<void>(() => { }));
            await client.registerEndpoint("test-endpoint", handler);
            await client.connect();
            await connection.sendToClient<messages.InvokeRequest>({ type: "Invoke", requestId: "1", endpoint: "test-endpoint" });
            await connection.sendToClient<messages.InvokeRequest>({ type: "Invoke", requestId: "2", endpoint: "test-endpoint" });

            expect(handler).toHaveBeenCalledTimes(2);
        });
    })

    describe("when the connection closes", () => {

        it("calls error on active subscribers", async () => {

            const client = new MessageRouterClient(connection, {});
            const subscriber: TopicSubscriber = {
                error: jest.fn()
            };
            
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            await client.subscribe("test-topic", subscriber);

            await new Promise(process.nextTick);
            connection.raiseClose();
            await new Promise(process.nextTick);

            expect(subscriber.error).toHaveBeenCalledExactlyOnceWith(expect.any(Error));
        });

        it("fails pending requests", async () => {

            const client = new MessageRouterClient(connection, {});
            const invokePromise = client.invoke("test-service");
            await new Promise(process.nextTick);
            connection.raiseClose();
            await new Promise(process.nextTick);

            await expect(invokePromise).rejects.toThrowWithName(MessageRouterError, ErrorNames.connectionAborted);
        });

    })

    describe("when the connection raises an error", () => {

        it("calls error on active subscribers", async () => {

            const client = new MessageRouterClient(connection, {});
            const subscriber: TopicSubscriber = {
                error: jest.fn()
            };

            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));
                
            await client.subscribe("test-topic", subscriber);

            const err = {};
            connection.raiseError(err);
            await new Promise(process.nextTick);

            expect(subscriber.error).toHaveBeenCalledExactlyOnceWith(err);
        });

        it("fails pending requests", async () => {

            const client = new MessageRouterClient(connection, {});
            await client.connect();

            const invokePromise = client.invoke("test-service");
            await new Promise(process.nextTick);
            const err = new Error("Fail");
            connection.raiseError(err);
            await new Promise(process.nextTick);

            await expect(invokePromise).rejects.toThrow("Fail");
        });

    });

    describe("when server raises error", () => {

        it("publish fails when PublishResponse contains error", async() => {
            const client = new MessageRouterClient(connection, {});
            connection.handle<messages.PublishMessage>(
                "Publish",
                msg => connection.sendToClient<messages.PublishResponse>({ type: "PublishResponse", requestId: msg.requestId, error: new MessageRouterError("testError-publish") }));

            var publishPromise = client.publish("test-topic", "test-payload", { correlationId: "test-correlation-id" });

            await expect(publishPromise).rejects.toThrowWithName(MessageRouterError, "testError-publish");
        });

        it("subscribe fails when SubscribeResponse contains error", async() => {
            const client = new MessageRouterClient(connection, {});
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId, error: new MessageRouterError("testError-subscribe") }));

            var subscribePromise = client.subscribe("test-topic", { next: () => { } });

            await expect(subscribePromise).rejects.toThrowWithName(MessageRouterError, "testError-subscribe");
        });

        it("dispose logs error when UnsubscribeResponse contains error", async() => {
            const client = new MessageRouterClient(connection, {});
            const consoleErrorMock = jest.spyOn(console, 'error').mockImplementation();
            connection.handle<messages.SubscribeMessage>(
                "Subscribe",
                msg => connection.sendToClient<messages.SubscribeResponse>({ type: "SubscribeResponse", requestId: msg.requestId }));

            connection.handle<messages.UnsubscribeMessage>(
                "Unsubscribe",
                msg => connection.sendToClient<messages.UnsubscribeResponse>({ type: "UnsubscribeResponse", requestId: msg.requestId, error: new MessageRouterError("testError-unsubscribe") }));

            var subscription = await client.subscribe("test-topic", { next: () => { } });
            await subscription.unsubscribe();

            // Waiting for the background task to finish.
            await new Promise(process.nextTick);
            await new Promise(process.nextTick);

            expect(consoleErrorMock).toHaveBeenCalled();
            expect(consoleErrorMock).toHaveBeenCalledWith("Exception thrown while unsubscribing.", new MessageRouterError("testError-unsubscribe"));
            consoleErrorMock.mockRestore();
        });
    });
})

type MockHandler<TMessage extends messages.Message> = ((msg: TMessage) => Promise<void>);

// This is a mock implementation that also installs spies on every method of the Connection interface.
// It responds to ConnectRequest by default.
class MockConnection implements Connection {

    constructor() {
        this.connect = jest.fn(this.connect);
        this.send = jest.fn(this.send);
        this.close = jest.fn(this.close);
        this.onMessage = jest.fn(this.onMessage);
        this.onError = jest.fn(this.onError);
        this.onClose = jest.fn(this.onClose);
    }

    connect(): Promise<void> {
        return Promise.resolve();
    }

    send(message: messages.Message): Promise<void> {
        const handler = this.messageHandlers[message.type];
        return handler ? handler(message) : Promise.resolve();
    }

    close(): Promise<void> {
        return Promise.resolve();
    }

    onMessage(callback: OnMessageCallback): void {
        this.onMessageCallback = callback;
    }

    onError(callback: OnErrorCallback): void {
        this.onErrorCallback = callback;
    }

    onClose(callback: OnCloseCallback): void {
        this.onCloseCallback = callback;
    }

    onMessageCallback?: OnMessageCallback;
    onErrorCallback?: OnErrorCallback;
    onCloseCallback?: OnCloseCallback;

    /**
     * The send method's default mock implementation will call the provided handler when the message has the specified type.
     * @param msgType 
     * @param handler 
     */
    handle<TMessage extends messages.Message>(msgType: messages.MessageType, handler: MockHandler<TMessage>): void {
        this.messageHandlers[msgType] = msg => handler(<TMessage>msg);
    }

    sendToClient<TMessage extends messages.Message>(msg: TMessage): Promise<void> {
        if (!this.onMessageCallback) throw new Error("onMessageCallback is not set");
        this.onMessageCallback(msg);
        return new Promise(process.nextTick);
    }

    raiseError(err: any) {
        if (!this.onErrorCallback) throw new Error("onErrorCallback is not set");

        return this.onErrorCallback(err);
    }

    raiseClose() {
        if (!this.onCloseCallback) throw new Error("onCloseCallback is not set");

        return this.onCloseCallback();
    }

    get mock(): jest.MockedObjectDeep<Connection> {
        return jest.mocked(this)
    };

    private messageHandlers: Record<string, MockHandler<messages.Message>> = {};
}
