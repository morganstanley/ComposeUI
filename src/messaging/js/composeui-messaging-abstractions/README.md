# @morgan-stanley/composeui-messaging-abstractions

Messaging helpers that wrap a lower‑level IMessaging abstraction (publish/subscribe plus request/response). Provides simple, typed APIs for sending and receiving structured data as JSON without duplicating serialization logic in callers. Provides JSON focused extension methods available using the `JsonMessaging` class.

## Features

- Thin adapter: JsonMessaging delegates to any IMessaging implementation.
- Typed publish/subscribe via JSON (subscribeJson / publishJson).
- Typed request/response services (registerJsonService / invokeJsonService / invokeJsonServiceNoRequest).
- Automatic serialization/deserialization.
- String short‑circuit: if a typed service handler returns a string it is passed through without double JSON.stringify.

## Installation

Install as a workspace dependency

```bash
npm install @morgan-stanley/composeui-messaging-abstractions
```

## Usage
Implement the `IMessaging` API to declare your own communication.

```typescript
import { IMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { HubConnection } from '@microsoft/signalr';

export class MyMessaging implements IMessaging {
    constructor(private readonly signalRMessaging: MySignalWrapper) {}
    //Implement the IMessging API...
}
```

## Module format

Rollup emits dual builds:
- ES Module: dist/index.js
- CommonJS: dist/index.cjs

 package.json exports map require to CJS and import to ESM. Use:
```typescript
// CJS
const { JsonMessaging } = require('@morgan-stanley/composeui-messaging-abstractions');

// ESM / TypeScript
import { JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';
```

## Dependencies

Runtime:
- rxjs (Unsubscribable type)

Build / Dev:
- typescript
- @rollup/plugin-typescript
- @rollup/plugin-node-resolve

## License

Apache-2.0 (see NOTICE and LICENSE files).

## Documentation

- [FDC3 Standard](https://fdc3.finos.org/)
- [ComposeUI Documentation](https://morganstanley.github.io/ComposeUI/)

&copy; Morgan Stanley. See NOTICE file for additional information.