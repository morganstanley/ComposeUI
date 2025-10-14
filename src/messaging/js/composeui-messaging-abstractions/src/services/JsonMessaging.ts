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

export class JsonMessaging implements IMessaging {
    constructor(private readonly messaging: IMessaging) {}

    async subscribe(topic: string, subscriber: TopicMessageHandler, cancellationToken?: AbortSignal): Promise<Unsubscribable> {
        return this.messaging.subscribe(topic, subscriber, cancellationToken);
    }

    async publish(topic: string, message: string, cancellationToken?: AbortSignal): Promise<void> {
        return this.messaging.publish(topic, message, cancellationToken);
    }

    async registerService(serviceName: string, serviceHandler: ServiceHandler, cancellationToken?: AbortSignal): Promise<AsyncDisposable> {
        return this.messaging.registerService(serviceName, serviceHandler, cancellationToken);
    }

    async invokeService(serviceName: string, payload?: string | null, cancellationToken?: AbortSignal): Promise<string | null> {
        return this.messaging.invokeService(serviceName, payload, cancellationToken);
    }

    async subscribeJson<TPayload>(
        topic: string,
        typedSubscriber: (payload: TPayload) => void | Promise<void>,
        cancellationToken?: AbortSignal
    ): Promise<Unsubscribable> {
        const jsonSubscriber: TopicMessageHandler = async(message: string): Promise<void> => {
            const payload = JSON.parse(message) as TPayload;
            await typedSubscriber(payload);
        };

        return this.messaging.subscribe(topic, jsonSubscriber, cancellationToken);
    }

    async publishJson<TPayload>(
        topic: string,
        payload: TPayload,
        cancellationToken?: AbortSignal
    ): Promise<void> {
        const stringPayload = JSON.stringify(payload);
        return this.messaging.publish(topic, stringPayload, cancellationToken);
    }

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

    async invokeJsonServiceNoRequest<TResult>(
        serviceName: string,
        cancellationToken?: AbortSignal
    ): Promise<TResult | null> {
        const response = await this.messaging.invokeService(serviceName, null, cancellationToken);

        return response == null ? null : JSON.parse(response) as TResult;
    }

    async registerJsonService<TRequest, TResult>(
        serviceName: string,
        typedHandler: TypedServiceHandler<TRequest, TResult>,
        cancellationToken?: AbortSignal
    ): Promise<AsyncDisposable> {
        const jsonServiceHandler = this.createJsonServiceHandler(typedHandler);
        return this.messaging.registerService(serviceName, jsonServiceHandler, cancellationToken);
    }

    // async subscribeJson<TPayload>(
    //     topic: string,
    //     handler: (payload: TPayload, context?: MessageContext) => void,
    //     cancellationToken?: AbortSignal
    // );
    // }

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
