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

import { IMessaging } from './IMessaging';
import { TopicMessageHandler, ServiceHandler, TypedServiceHandler } from './Delegates';
import { Unsubscribable } from 'rxjs';

/**
 * JSON wrapper around a lower-level IMessaging implementation.
 * Provides typed helpers for publish/subscribe and request/response using JSON serialization.
 */
export class JsonMessaging implements IMessaging {
    /**
     * Creates a new JsonMessaging adapter.
     * @param messaging Underlying messaging implementation handling raw string messages.
     */
    constructor(private readonly messaging: IMessaging) {}

    /**
     * Subscribes to a topic with a raw string handler.
     * @param topic Topic identifier.
     * @param subscriber Callback invoked with each serialized message string.
     * @param cancellationToken Optional abort signal for subscription setup.
     * @returns Promise resolving to an Unsubscribable to stop receiving messages.
     */
    async subscribe(topic: string, subscriber: TopicMessageHandler, cancellationToken?: AbortSignal): Promise<Unsubscribable> {
        return this.messaging.subscribe(topic, subscriber, cancellationToken);
    }

    /**
     * Publishes a raw string message.
     * @param topic Topic identifier.
     * @param message Serialized message string.
     * @param cancellationToken Optional abort signal.
     */
    async publish(topic: string, message: string, cancellationToken?: AbortSignal): Promise<void> {
        return this.messaging.publish(topic, message, cancellationToken);
    }

    /**
     * Registers a raw service handler.
     * @param serviceName Service name used for invocation.
     * @param serviceHandler Handler operating on serialized request/response strings.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to an AsyncDisposable for unregistering.
     */
    async registerService(serviceName: string, serviceHandler: ServiceHandler, cancellationToken?: AbortSignal): Promise<AsyncDisposable> {
        return this.messaging.registerService(serviceName, serviceHandler, cancellationToken);
    }

    /**
     * Invokes a raw service.
     * @param serviceName Service name.
     * @param payload Optional serialized request payload or null.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to a serialized response or null.
     */
    async invokeService(serviceName: string, payload?: string | null, cancellationToken?: AbortSignal): Promise<string | null> {
        return this.messaging.invokeService(serviceName, payload, cancellationToken);
    }

    /**
     * Subscribes with a typed JSON payload handler.
     * @typeParam TPayload Deserialized payload type.
     * @param topic Topic identifier.
     * @param typedSubscriber Callback receiving the typed payload.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to an Unsubscribable for the subscription.
     */
    async subscribeJson<TPayload>(
        topic: string,
        typedSubscriber: (payload: TPayload) => void | Promise<void>,
        cancellationToken?: AbortSignal
    ): Promise<Unsubscribable> {
        const jsonSubscriber: TopicMessageHandler = async (message: string): Promise<void> => {
            const payload = JSON.parse(message) as TPayload;
            await typedSubscriber(payload);
        };

        return this.messaging.subscribe(topic, jsonSubscriber, cancellationToken);
    }

    /**
     * Publishes a typed payload by JSON serializing it.
     * @typeParam TPayload Payload type.
     * @param topic Topic identifier.
     * @param payload Typed payload instance.
     * @param cancellationToken Optional abort signal.
     */
    async publishJson<TPayload>(
        topic: string,
        payload: TPayload,
        cancellationToken?: AbortSignal
    ): Promise<void> {
        const stringPayload = JSON.stringify(payload);
        return this.messaging.publish(topic, stringPayload, cancellationToken);
    }

    /**
     * Invokes a service with a typed request and typed response.
     * @typeParam TPayload Request type.
     * @typeParam TResult Response type.
     * @param serviceName Service name.
     * @param payload Typed request payload.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to typed response or null.
     */
    async invokeJsonService<TPayload, TResult>(
        serviceName: string,
        payload: TPayload,
        cancellationToken?: AbortSignal
    ): Promise<TResult | null> {
        const stringPayload = JSON.stringify(payload);
        const response = await this.messaging.invokeService(serviceName, stringPayload, cancellationToken);

        if (response == null) {
            return null;
        }

        return JSON.parse(response) as TResult;
    }

    /**
     * Invokes a service that expects no request body.
     * @typeParam TResult Response type.
     * @param serviceName Service name.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to typed response or null.
     */
    async invokeJsonServiceNoRequest<TResult>(
        serviceName: string,
        cancellationToken?: AbortSignal
    ): Promise<TResult | null> {
        const response = await this.messaging.invokeService(serviceName, null, cancellationToken);

        return response == null ? null : JSON.parse(response) as TResult;
    }

    /**
     * Registers a typed JSON service handler.
     * @typeParam TRequest Request type.
     * @typeParam TResult Response type.
     * @param serviceName Service name to register.
     * @param typedHandler Handler receiving a typed request and returning a typed response or null.
     * @param cancellationToken Optional abort signal.
     * @returns Promise resolving to an AsyncDisposable for unregistering.
     */
    async registerJsonService<TRequest, TResult>(
        serviceName: string,
        typedHandler: TypedServiceHandler<TRequest, TResult>,
        cancellationToken?: AbortSignal
    ): Promise<AsyncDisposable> {
        const jsonServiceHandler = this.createJsonServiceHandler(typedHandler);
        return this.messaging.registerService(serviceName, jsonServiceHandler, cancellationToken);
    }

    /**
     * Creates an internal raw service handler that performs JSON serialization/deserialization.
     * @typeParam TRequest Request type.
     * @typeParam TResult Response type.
     * @param realHandler Typed handler to wrap.
     * @returns A ServiceHandler operating on serialized strings.
     */
    private createJsonServiceHandler<TRequest, TResult>(
        realHandler: TypedServiceHandler<TRequest, TResult>
    ): ServiceHandler {
        return async (payload?: string | null): Promise<string | null> => {
            const request = payload == null ? null : JSON.parse(payload) as TRequest;
            const result = await realHandler(request);

            if (typeof result === 'string') {
                return result;
            }

            return result == null ? null : JSON.stringify(result);
        };
    }
}