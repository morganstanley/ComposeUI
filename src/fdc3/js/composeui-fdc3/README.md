<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# composeui-fdc3

`composeui-fdc3` is a TypeScript/JavaScript package that provides [FDC3](https://fdc3.finos.org/) support for [ComposeUI](https://morganstanley.github.io/ComposeUI/) applications. It enables interoperability between desktop applications by implementing the FDC3 standard APIs for context sharing, intent handling, and application discovery.

**Note:** This package currently supports only FDC3 version 2.0.  
It uses the [Messaging abstractions TypeScript library](https://github.com/morganstanley/ComposeUI/tree/main/src/messaging/js/composeui-messaging-abstractions) as its messaging layer.

## Features

- Implements FDC3 APIs for context, intents, and app channels
- TypeScript typings included

## Installation

You need to embed the generated bundle into your shell application, similar to how it is handled in the current Shell POC. In that example, a .NET WPF application includes the bundle as an embedded resource. When using WebView2 to load web windows, the application initializes the bundle as a script and injects it into the page.

To use the FDC3 APIs, install the official FINOS FDC3 library (version 2.0) in your application:

```sh
npm install @finos/fdc3@2.0.0
```

## Usage

Import and use the FDC3 API in your application as needed.

```typescript
// Raise an intent
fdc3.raiseIntent('ViewChart', { instrument: { type: 'fdc3.instrument', id: { ticker: 'AAPL' } } });

// Broadcast context
fdc3.broadcast({ type: 'fdc3.contact', name: 'Jane Doe' });

// Listen for context
fdc3.addContextListener('fdc3.instrument', context => {
  console.log('Received instrument context:', context);
});
```

## API

This package implements the [FDC3 2.0+ API](https://fdc3.finos.org/docs/2.0/api/spec). See the FDC3 documentation for details on available methods and context types.

## Setup

- Have a Shell bundling the generated resource into the webpage.
- Import and use the FDC3 API from the FINOS library as shown above.
- Ensure your environment is configured to support FDC3 interoperability, e.g., having the FDC3 DesktopAgent backend for handling the requests.

## Documentation

- [FDC3 Standard](https://fdc3.finos.org/)
- [ComposeUI Documentation](https://morganstanley.github.io/ComposeUI/)

&copy; Morgan Stanley. See NOTICE file for additional information.