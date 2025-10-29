(function () {
    'use strict';

    class AsyncDisposableWrapper {
        constructor(messageRouterClient, serviceName) {
            this.messageRouterClient = messageRouterClient;
            this.serviceName = serviceName;
        }
        [Symbol.asyncDispose]() {
            return this.messageRouterClient.unregisterService(this.serviceName);
        }
    }

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
    /**
     * Implementation of IMessaging interface using MessageRouter.
     * Provides messaging capabilities through the MessageRouter client for ComposeUI applications.
     */
    class MessageRouterMessaging {
        /**
         * Creates a new instance of MessageRouterMessaging.
         * @param messageRouterClient The MessageRouter client instance to use for communication.
         */
        constructor(messageRouterClient) {
            this.messageRouterClient = messageRouterClient;
        }
        /**
         * Subscribes to messages on a specific topic.
         * @param topic The topic to subscribe to.
         * @param subscriber Callback function that will be invoked with each received message.
         * @param cancellationToken Optional signal to cancel the subscription setup.
         * @returns A Promise that resolves to an Unsubscribable object for managing the subscription.
         * @remarks If a message is received without a payload, a warning will be logged and the subscriber will not be called.
         */
        subscribe(topic, subscriber, cancellationToken) {
            return this.messageRouterClient.subscribe(topic, (message) => {
                if (!message.payload) {
                    console.warn(`Received empty message on topic ${topic}`);
                    return;
                }
                subscriber(message.payload);
            });
        }
        /**
         * Publishes a message to a specific topic.
         * @param topic The topic to publish to.
         * @param message The message content to publish.
         * @param cancellationToken Optional signal to cancel the publish operation.
         * @returns A Promise that resolves when the message has been published.
         */
        publish(topic, message, cancellationToken) {
            return this.messageRouterClient.publish(topic, message);
        }
        /**
         * Registers a service handler for a specific service name.
         * @param serviceName The name of the service to register.
         * @param serviceHandler The handler function that will process service requests.
         * @param cancellationToken Optional signal to cancel the service registration.
         * @returns A Promise that resolves to an AsyncDisposable for managing the service registration.
         * @remarks The service handler will receive the payload from the request and should return a response.
         * Both the payload and response can be null.
         */
        async registerService(serviceName, serviceHandler, cancellationToken) {
            await this.messageRouterClient.registerService(serviceName, async (endpoint, payload, context) => {
                const result = await serviceHandler(payload);
                return result;
            });
            const disposable = new AsyncDisposableWrapper(this.messageRouterClient, serviceName);
            return disposable;
        }
        /**
         * Invokes a registered service.
         * @param serviceName The name of the service to invoke.
         * @param payload Optional payload to send with the service request.
         * @param cancellationToken Optional signal to cancel the service invocation.
         * @returns A Promise that resolves to the service response or null if no response is received.
         * @remarks If the payload is null, the service will be invoked without a payload.
         * The response will be null if the service doesn't return a response or if an error occurs.
         */
        async invokeService(serviceName, payload, cancellationToken) {
            if (payload == null) {
                const result = await this.messageRouterClient.invoke(serviceName);
                if (!result) {
                    return null;
                }
                return result;
            }
            const response = await this.messageRouterClient.invoke(serviceName, payload);
            if (!response) {
                return null;
            }
            return response;
        }
    }

    /**
       * @license
       * author: Morgan Stanley
       * composeui-messaging-client.js v0.1.0-alpha.9
       * Released under the Apache-2.0 license.
       */

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
    const ErrorNames = {
        duplicateEndpoint: "DuplicateEndpoint",
        duplicateRequestId: "DuplicateRequestId",
        invalidEndpoint: "InvalidEndpoint",
        invalidTopic: "InvalidTopic",
        unknownEndpoint: "UnknownEndpoint",
        connectionClosed: "ConnectionClosed",
        connectionAborted: "ConnectionAborted",
        connectionFailed: "ConnectionFailed",
    };

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
    class WebSocketConnection {
        options;
        constructor(options) {
            this.options = options;
        }
        connect() {
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
        send(message) {
            if (!this.websocket) {
                return Promise.reject();
            }
            this.websocket.send(JSON.stringify(message));
            return Promise.resolve();
        }
        close() {
            if (this.isConnected) {
                this.websocket?.close(1000, "Closed by client");
                this.isConnected = false;
                delete this.websocket;
            }
            return Promise.resolve();
        }
        onMessage(callback) {
            this._onMessage = callback;
        }
        onError(callback) {
            this._onError = callback;
        }
        onClose(callback) {
            this._onClose = callback;
        }
        static deserializeMessage(data) {
            const msg = JSON.parse(data);
            return msg;
        }
        websocket;
        isConnected = false;
        messageQueue = [];
        _onMessage;
        _onError;
        _onClose;
    }

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
    class Deferred {
        constructor() {
            this.promise = new Promise((resolve, reject) => {
                this.resolve =
                    (value) => {
                        this.settle();
                        resolve(value);
                    };
                this.reject =
                    (reason) => {
                        this.settle();
                        reject(reason);
                    };
            });
        }
        resolve = () => { };
        reject = () => { };
        promise;
        settle() {
            const noop = () => { };
            this.resolve = noop;
            this.reject = noop;
        }
    }

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
    function isProtocolError(err) {
        return (typeof err === "object") && ("name" in err);
    }

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
    class MessageRouterError extends Error {
        constructor(err, message, stack) {
            let [name, msg] = isProtocolError(err) ? [err.name, err.message] : [err, message];
            super(msg);
            this.name = name;
            if (stack) {
                this.stack = stack;
            }
        }
    }
    function createProtocolError(err) {
        if (typeof err === "string")
            return {
                name: "Error",
                message: err
            };
        return {
            name: err.name ?? "Error",
            message: err.message ?? `Unknown error (${err})`
        };
    }

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
    class ThrowHelper {
        static duplicateEndpoint(endpoint) {
            return new MessageRouterError({ name: ErrorNames.duplicateEndpoint, message: `Duplicate endpoint registration: '${endpoint}'` });
        }
        static duplicateRequestId() {
            return new MessageRouterError({ name: ErrorNames.duplicateRequestId, message: "Duplicate request ID" });
        }
        static invalidEndpoint(endpoint) {
            return new MessageRouterError({ name: ErrorNames.invalidEndpoint, message: `Invalid endpoint: '${endpoint}'` });
        }
        static invalidTopic(topic) {
            return new MessageRouterError({ name: ErrorNames.invalidTopic, message: `Invalid topic: '${topic}'` });
        }
        static unknownEndpoint(endpoint) {
            return new MessageRouterError({ name: ErrorNames.unknownEndpoint, message: `Unknown endpoint: ${endpoint}` });
        }
        static connectionClosed() {
            return new MessageRouterError({ name: ErrorNames.connectionClosed, message: "The connection has been closed" });
        }
        static connectionFailed(message) {
            return new MessageRouterError({ name: ErrorNames.connectionFailed, message: `Connection failed with message ${message}` });
        }
        static connectionAborted() {
            return new MessageRouterError({ name: ErrorNames.connectionAborted, message: "The connection dropped unexpectedly" });
        }
    }

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
    function isResponse(message) {
        return (message.type === "InvokeResponse"
            || message.type === "RegisterServiceResponse"
            || message.type === "UnregisterServiceResponse"
            || message.type === "SubscribeResponse"
            || message.type === "UnsubscribeResponse"
            || message.type === "PublishResponse");
    }

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
    function isConnectResponse(message) {
        return message.type == "ConnectResponse";
    }

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
    function isInvokeRequest(message) {
        return message.type == "Invoke";
    }

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
    function isTopicMessage(message) {
        return message.type == "Topic";
    }

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
    var ClientState;
    (function (ClientState) {
        ClientState[ClientState["Created"] = 0] = "Created";
        ClientState[ClientState["Connecting"] = 1] = "Connecting";
        ClientState[ClientState["Connected"] = 2] = "Connected";
        ClientState[ClientState["Closing"] = 3] = "Closing";
        ClientState[ClientState["Closed"] = 4] = "Closed";
    })(ClientState || (ClientState = {}));
    class MessageRouterClient {
        connection;
        options;
        constructor(connection, options) {
            this.connection = connection;
            this.options = options;
            this.options = options ?? {};
        }
        _clientId;
        get clientId() {
            return this._clientId;
        }
        connect() {
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
        close() {
            return this.closeCore();
        }
        async subscribe(topicName, subscriber) {
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
                    await this.sendRequest({
                        requestId: this.getRequestId(),
                        type: "Subscribe",
                        topic: topicName
                    });
                }
                catch (error) {
                    delete this.topics[topicName];
                    throw error;
                }
            }
            return subscription;
        }
        async publish(topic, payload, options) {
            this.checkState();
            await this.sendRequest({
                type: "Publish",
                requestId: this.getRequestId(),
                topic,
                payload,
                correlationId: options?.correlationId
            });
        }
        async invoke(endpoint, payload, options) {
            this.checkState();
            const response = await this.sendRequest({
                type: "Invoke",
                requestId: this.getRequestId(),
                endpoint,
                payload,
                correlationId: options?.correlationId
            });
            return response.payload;
        }
        async registerService(endpoint, handler, descriptor) {
            this.checkState();
            if (this.endpointHandlers[endpoint])
                throw ThrowHelper.duplicateEndpoint(endpoint);
            this.endpointHandlers[endpoint] = handler;
            try {
                await this.sendRequest({
                    type: "RegisterService",
                    requestId: this.getRequestId(),
                    endpoint,
                    descriptor
                });
            }
            catch (error) {
                delete this.endpointHandlers[endpoint];
                throw error;
            }
        }
        async unregisterService(endpoint) {
            this.checkState();
            if (!this.endpointHandlers[endpoint])
                return;
            await this.sendRequest({
                type: "UnregisterService",
                requestId: this.getRequestId(),
                endpoint
            });
            delete this.endpointHandlers[endpoint];
        }
        registerEndpoint(endpoint, handler, descriptor) {
            this.checkState();
            if (this.endpointHandlers[endpoint])
                throw ThrowHelper.duplicateEndpoint(endpoint);
            this.endpointHandlers[endpoint] = handler;
            return Promise.resolve();
        }
        unregisterEndpoint(endpoint) {
            this.checkState();
            delete this.endpointHandlers[endpoint];
            return Promise.resolve();
        }
        get state() {
            return this._state;
        }
        _state = ClientState.Created;
        lastRequestId = 0;
        topics = {};
        connected = new Deferred();
        closed = new Deferred();
        pendingRequests = {};
        endpointHandlers = {};
        pendingUnsubscriptions = {};
        async connectCore() {
            this._state = ClientState.Connecting;
            try {
                this.connection.onMessage(this.handleMessage.bind(this));
                this.connection.onError(this.handleError.bind(this));
                this.connection.onClose(this.handleClose.bind(this));
                await this.connection.connect();
                const req = {
                    type: "Connect",
                    accessToken: this.options.accessToken
                };
                await this.connection.send(req);
                // This must be the last statement before catch so that awaiting `connected` 
                // has the same effect as awaiting `connect()`. `close()` also rejects this promise.
                await this.connected.promise;
            }
            catch (error) {
                if (error instanceof MessageRouterError) {
                    throw error;
                }
                else {
                    await this.closeCore(error);
                    throw ThrowHelper.connectionFailed(error.message || error);
                }
            }
        }
        async closeCore(error) {
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
                console.error(e);
            }
            this._state = ClientState.Closed;
            this.closed.resolve();
        }
        failPendingRequests(error) {
            for (let requestId in this.pendingRequests) {
                this.pendingRequests[requestId].reject(error);
                delete this.pendingRequests[requestId];
            }
        }
        async failSubscribers(error) {
            for (let topicName in this.topics) {
                const topic = this.topics[topicName];
                topic.error(error);
            }
        }
        async sendMessage(message) {
            await this.connect();
            await this.connection.send(message);
        }
        async sendRequest(request) {
            const deferred = this.pendingRequests[request.requestId] = new Deferred();
            try {
                await this.sendMessage(request);
            }
            catch (error) {
                delete this.pendingRequests[request.requestId];
                throw error;
            }
            return await deferred.promise;
        }
        handleMessage(message) {
            if (isTopicMessage(message)) {
                this.handleTopicMessage(message);
                return;
            }
            if (isResponse(message)) {
                this.handleResponse(message);
                return;
            }
            if (isInvokeRequest(message)) {
                this.handleInvokeRequest(message);
                return;
            }
            if (isConnectResponse(message)) {
                this.handleConnectResponse(message);
                return;
            }
        }
        handleTopicMessage(message) {
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
        handleResponse(message) {
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
        async handleInvokeRequest(message) {
            try {
                const handler = this.endpointHandlers[message.endpoint];
                if (!handler)
                    throw ThrowHelper.unknownEndpoint(message.endpoint);
                const result = await handler(message.endpoint, message.payload, {
                    sourceId: message.sourceId,
                    correlationId: message.correlationId
                });
                await this.sendMessage({
                    type: "InvokeResponse",
                    requestId: message.requestId,
                    payload: typeof result === "string" ? result : undefined
                });
            }
            catch (error) {
                await this.sendMessage({
                    type: "InvokeResponse",
                    requestId: message.requestId,
                    error: createProtocolError(error)
                });
            }
        }
        handleConnectResponse(message) {
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
        checkState() {
            if (this._state == ClientState.Closed || this._state == ClientState.Closing) {
                throw ThrowHelper.connectionClosed();
            }
        }
        handleError(error) {
            switch (this._state) {
                case ClientState.Closing:
                case ClientState.Closed:
                    return;
            }
            this.closeCore(error);
        }
        handleClose() {
            this.handleError(ThrowHelper.connectionAborted());
        }
        async unsubscribe(topicName) {
            let topic = this.topics[topicName];
            if (!topic)
                return;
            if (this.pendingUnsubscriptions[topicName]) {
                await this.pendingUnsubscriptions[topicName];
            }
            this.pendingUnsubscriptions[topicName] = this.sendRequest({
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
        getRequestId() {
            return '' + (++this.lastRequestId);
        }
    }
    class Topic {
        constructor(onUnsubscribe) {
            this.onUnsubscribe = onUnsubscribe;
        }
        subscribe(subscriber) {
            if (this.isCompleted)
                return {
                    unsubscribe: () => { }
                };
            this.subscribers.push(subscriber);
            return {
                unsubscribe: () => this.unsubscribe(subscriber)
            };
        }
        unsubscribe(subscriber) {
            if (this.isCompleted)
                return;
            const idx = this.subscribers.lastIndexOf(subscriber);
            if (idx < 0)
                return;
            this.subscribers.splice(idx, 1);
            if (this.subscribers.length == 0) {
                this.onUnsubscribe();
            }
        }
        next(message) {
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
        error(error) {
            if (this.isCompleted)
                return;
            this.isCompleted = true;
            for (let subscriber of this.subscribers) {
                try {
                    subscriber.error?.call(subscriber, error);
                }
                catch (e) {
                    console.error(e);
                }
            }
        }
        complete() {
            if (this.isCompleted)
                return;
            for (let subscriber of this.subscribers) {
                try {
                    subscriber.complete?.call(subscriber);
                }
                catch (e) {
                    console.error(e);
                }
            }
        }
        isCompleted = false;
        onUnsubscribe;
        subscribers = [];
    }

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
    function createMessageRouter(config) {
        config ??= window.composeui?.messageRouterConfig;
        if (config?.webSocket) {
            const connection = new WebSocketConnection(config.webSocket);
            return new MessageRouterClient(connection, config);
        }
        throw ConfigNotFound();
        function ConfigNotFound() {
            return new Error("Unable to create the MessageRouter client, configuration is missing.");
        }
    }

    window.composeui.messaging.communicator = new MessageRouterMessaging(createMessageRouter());

})();
