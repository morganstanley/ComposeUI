# ComposeUI Messaging

A TypeScript implementation of the messaging abstraction library using MessageRouter client library.

## Installation
This is not an npm package that can be installed. You need to embed the generated bundle into your shell application, similar to how it is handled in the current Shell POC. In that example, a .NET WPF application includes the bundle as an embedded resource. When using WebView2 to load web windows, the application initializes the bundle as a script and injects it into the page.
To use this you'll need a system that use messaging abstraction library, and get the implementation from the window.composeui.messaging.communicator object.

## Dependencies

- @morgan-stanley/composeui-messaging-abstractions
- @morgan-stanley/composeui-messaging-client
- rxjs

## Usage

### Basic Messaging

```typescript

import { IMessaging } from '@morgan-stanley/composeui-messaging-abstractions';
// Initialize the messaging system
const messaging = window.composeui.messaging.communicator

// Subscribe to a topic
const subscription = await messaging.subscribe('my-topic', (message) => {
    console.log('Received:', message);
});

// Publish to a topic
await messaging.publish('my-topic', 'Hello, World!');

// Unsubscribe when done
subscription.unsubscribe();
```

### Service Registration and Invocation

```typescript
// Register a service
const disposable = await messaging.registerService('my-service', async (request) => {
    return `Processed: ${request}`;
});

// Invoke a service
const response = await messaging.invokeService('my-service', 'test request');
console.log(response); // Output: "Processed: test request"

// Unregister the service when done
await disposable.dispose();
```

### Using with JSON Messaging

```typescript
import { JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';

// Create a JSON-enabled messaging wrapper
const jsonMessaging = new JsonMessaging(messaging);

// Work with typed objects
interface MyData {
    id: number;
    value: string;
}

// Subscribe with type safety
await jsonMessaging.subscribeJson<MyData>('my-topic', (data) => {
    console.log(data.id, data.value);
});

// Publish typed objects
await jsonMessaging.publishJson('my-topic', {
    id: 1,
    value: 'test'
});
```

## API Reference

### MessageRouterMessaging

The main implementation of the `IMessaging` interface using the MessageRouter client.

#### Methods

- `subscribe(topic: string, subscriber: TopicMessageHandler, cancellationToken?: AbortSignal): Promise<Unsubscribable>`
  - Subscribes to messages on a specific topic
  - Returns an unsubscribable object to cancel the subscription
  - TopicMessageHandler is a function that takes a string and returns a Promise of void

- `publish(topic: string, message: string, cancellationToken?: AbortSignal): Promise<void>`
  - Publishes a message to a specific topic

- `registerService(serviceName: string, serviceHandler: ServiceHandler, cancellationToken?: AbortSignal): Promise<AsyncDisposable>`
  - Registers a service handler
  - Returns a disposable object to unregister the service
  - ServiceHandler is function that either takes string or null and returns a Promise of string or null

- `invokeService(serviceName: string, payload?: string | null, cancellationToken?: AbortSignal): Promise<string | null>`
  - Invokes a registered service
  - Returns the service response or null

## Contributing

Please see the [Contributing Guide](../../../CONTRIBUTING.md) for guidelines on how to contribute to this project.

## License

Apache License 2.0 - See [LICENSE](../../../LICENSE) for more information.
