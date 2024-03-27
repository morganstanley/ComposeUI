# @morgan-stanley/composeui-messaging-client
This package contains the client used to connect to the MessageRouter from web modules in ComposeUI.

## Setup
Import createMessageRouter from the package

```
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";
```

Use createMessageRouter() to instantiate the MessageRouter client in a ComposeUI application. It will connect to the MessageRouter hosted by the container when you call connect() on the client.

```
let client = createMessageRouter();
await client.connect();
```

## Usage

### Subscribe to a topic
Use the subscribe method of the client to set a handler on a topic. The message parameter of the handler method contains the payload as a string.
The following example parses a JSON payload from the "exampleTopic" topic and logs it to console.

```
client.subscribe('exampleTopic', (message) => {
    const payload = JSON.parse(message.payload);
    console.log(payload);
    });
```

### Publish a message
Use the publish method of the client to publish a message to a topic. The payload of the message must be a string.
The following example creates a JSON string out of an object, and publishes it to the "exampleTopic" topic.

```
await client.publish('exampleTopic', JSON.stringify(payload));
```