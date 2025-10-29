import { IMessaging, ServiceHandler, TopicMessageHandler } from "@morgan-stanley/composeui-messaging-abstractions";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { Unsubscribable } from "rxjs";
/**
 * Implementation of IMessaging interface using MessageRouter.
 * Provides messaging capabilities through the MessageRouter client for ComposeUI applications.
 */
export declare class MessageRouterMessaging implements IMessaging {
    private readonly messageRouterClient;
    /**
     * Creates a new instance of MessageRouterMessaging.
     * @param messageRouterClient The MessageRouter client instance to use for communication.
     */
    constructor(messageRouterClient: MessageRouter);
    /**
     * Subscribes to messages on a specific topic.
     * @param topic The topic to subscribe to.
     * @param subscriber Callback function that will be invoked with each received message.
     * @param cancellationToken Optional signal to cancel the subscription setup.
     * @returns A Promise that resolves to an Unsubscribable object for managing the subscription.
     * @remarks If a message is received without a payload, a warning will be logged and the subscriber will not be called.
     */
    subscribe(topic: string, subscriber: TopicMessageHandler, cancellationToken?: AbortSignal): Promise<Unsubscribable>;
    /**
     * Publishes a message to a specific topic.
     * @param topic The topic to publish to.
     * @param message The message content to publish.
     * @param cancellationToken Optional signal to cancel the publish operation.
     * @returns A Promise that resolves when the message has been published.
     */
    publish(topic: string, message: string, cancellationToken?: AbortSignal): Promise<void>;
    /**
     * Registers a service handler for a specific service name.
     * @param serviceName The name of the service to register.
     * @param serviceHandler The handler function that will process service requests.
     * @param cancellationToken Optional signal to cancel the service registration.
     * @returns A Promise that resolves to an AsyncDisposable for managing the service registration.
     * @remarks The service handler will receive the payload from the request and should return a response.
     * Both the payload and response can be null.
     */
    registerService(serviceName: string, serviceHandler: ServiceHandler, cancellationToken?: AbortSignal): Promise<AsyncDisposable>;
    /**
     * Invokes a registered service.
     * @param serviceName The name of the service to invoke.
     * @param payload Optional payload to send with the service request.
     * @param cancellationToken Optional signal to cancel the service invocation.
     * @returns A Promise that resolves to the service response or null if no response is received.
     * @remarks If the payload is null, the service will be invoked without a payload.
     * The response will be null if the service doesn't return a response or if an error occurs.
     */
    invokeService(serviceName: string, payload?: string | null, cancellationToken?: AbortSignal): Promise<string | null>;
}
