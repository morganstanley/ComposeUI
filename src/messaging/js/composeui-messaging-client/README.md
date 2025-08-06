# @morgan-stanley/composeui-messaging-client

This package provides a JavaScript/TypeScript client for connecting to the ComposeUI MessageRouter from web modules. It enables publish/subscribe messaging and service invocation between web applications and the ComposeUI messaging infrastructure.

## Features

- Connect to the ComposeUI MessageRouter from browser or Electron environments
- Publish and subscribe to topics
- Register and invoke services
- Promise-based async API
- TypeScript typings included

## Setup

Install the package:

```sh
npm install @morgan-stanley/composeui-messaging-client
```

Import and create a client:

```typescript
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";

let client = createMessageRouter();
await client.connect();
```

## Usage

### Subscribe to a topic

```typescript
client.subscribe('exampleTopic', (message) => {
    const payload = JSON.parse(message.payload);
    console.log(payload);
});
```

### Publish a message

```typescript
await client.publish('exampleTopic', JSON.stringify({ foo: "bar" }));
```

### Register a service

```typescript
await client.registerService('myService', async (endpoint, payload, context) => {
    // handle the request and return a response string
    return "response";
});
```

### Invoke a service

```typescript
const response = await client.invoke('myService', "request payload");
console.log(response); // response string from the service
```

### Unsubscribe from a topic

```typescript
const subscription = client.subscribe('exampleTopic', handler);
// Later, to unsubscribe:
subscription.unsubscribe();
```

# API Reference

## `createMessageRouter(options?)`
Creates a new MessageRouter client instance.

- `options` (optional): Configuration object for the client.

### `client.connect() => Promise<void>`
Establishes a connection to the MessageRouter.

- Returns: `Promise<void>`

### `client.close() => Promise<void>`
Closes the connection to the MessageRouter.

- Returns: `Promise<void>`

### `client.subscribe(topic: string, subscriber: TopicSubscriber) => Promise<Unsubscribable>`
Subscribes to a topic and registers a handler for incoming messages.

- `topic`: The topic string to subscribe to.
- `handler`: Function called with each message received:  `PartialObserver<TopicMessage> | ((message: TopicMessage) => void)`.
- Returns: `Unsubscribable` (call `unsubscribe()` to remove the handler)

### `client.publish(topic: string, payload: MessageBuffer, options?: PublishOptions) => Promise<void>`
Publishes a message to a topic.

- `topic`: The topic string to publish to.
- `payload`: The message payload as a string.
- `options`: Additional publish options (`correlationId`).
- Returns: `Promise<void>`

### `client.invoke(service: string, payload: MessageBuffer, options?: InvokeOptions) => Promise<MessageBuffer | undefined>`
Invokes a service and returns the response.

- `service`: The service name to invoke.
- `payload`: The request payload as a string.
- `options`: Additional invoke options (`correlationId`).
- Returns: `Promise<string | undefined>` (the response payload)

### `client.registerService(service: string, handler: MessageHandler, descriptor?: EndpointDescriptor | undefined) => Promise<void>)`
Registers a service handler for a given service name.

- `service`: The service name to register.
- `handler`: Async function that receives the endpoint, request payload, and context, and returns a response payload. `(endpoint: string, payload: MessageBuffer | undefined, context: MessageContext) => (MessageBuffer | Promise<MessageBuffer> | void | Promise<void>)`
- `descriptor`: Additional description for the endpoint
- Returns: `Promise<void>`

### `client.unregisterService(service: string) => Promise<void>`
Unregisters a previously registered service.

- `service`: The service name to unregister.
- Returns: `Promise<void>`

### `client.registerEndpoint(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor | undefined) => Promise<void>`
Registers a local endpoint handler that is not advertised to other clients.

- `endpoint`: The endpoint name to register.
- `handler`: Async function that receives the endpoint, request payload, and context, and returns a response payload. `(endpoint: string, payload: MessageBuffer | undefined, context: MessageContext) => (MessageBuffer | Promise<MessageBuffer> | void | Promise<void>)`.
- `descriptor` (optional): Endpoint descriptor object containing additional description for the endpoint.
- Returns: `Promise<void>`

### `client.unregisterEndpoint(endpoint: string) => Promise<void>`
Unregisters a previously registered local endpoint.

- `endpoint`: The endpoint name to unregister.
- Returns: `Promise<void>`

### `client.state()`
Returns the current connection state of the client.

- Type: `ClientState` (`Created`, `Connecting`, `Connected`, `Closing`, `Closed`)
- Usage example:
  ```typescript
  if (client.state === ClientState.Connected) {
      // Client is connected
  }
  ```

---

&copy; Morgan Stanley. See NOTICE file for additional information.
