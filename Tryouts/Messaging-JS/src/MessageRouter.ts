import { Unsubscribable } from "rxjs";
import { EndpointDescriptor, InvokeOptions, MessageBuffer, MessageHandler, PublishOptions, TopicMessage, TopicSubscriber } from ".";

export interface MessageRouter {
    connect(): Promise<void>;
    subscribe(topic: string, subscriber: TopicSubscriber | ((message: TopicMessage) => void)): Promise<Unsubscribable>;
    publish(topic: string, payload?: MessageBuffer, options?: PublishOptions): Promise<void>;
    invoke(endpoint: string, payload?: MessageBuffer, options?: InvokeOptions): Promise<MessageBuffer | undefined>;
    registerService(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void>;
    unregisterService(endpoint: string): Promise<void>;
    registerEndpoint(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void>;
    unregisterEndpoint(endpoint: string): Promise<void>;
}
