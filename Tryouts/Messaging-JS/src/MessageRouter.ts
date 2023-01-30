import { Unsubscribable } from "rxjs";
import { PublishOptions } from "./PublishOptions";
import { EndpointDescriptor } from "./EndpointDescriptor";
import { InvokeOptions } from "./InvokeOptions";
import { MessageBuffer } from "./MessageBuffer";
import { MessageHandler } from "./MessageHandler";
import { TopicMessage } from "./TopicMessage";
import { TopicSubscriber } from "./TopicSubscriber";

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
