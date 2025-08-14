# MorganStanley.ComposeUI.Messaging.Client

`MorganStanley.ComposeUI.Messaging.Client` is a .NET client library for connecting to a ComposeUI Message Router. It enables .NET applications to send and receive messages using the ComposeUI messaging infrastructure, supporting scalable and decoupled communication between distributed components.

## Features

- Connects to a ComposeUI Message Router for inter-process and inter-service messaging
- Implements the `IMessageRouter` interface for advanced messaging scenarios
- Supports dependency injection with provided DI extension methods
- Designed for reliability and scalability in distributed systems
- Targets .NET 8.0

## Installation

Install via NuGet:

```shell
dotnet package add MorganStanley.ComposeUI.Messaging.Client
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.Client
```

## Usage

### 1. Register the Client in Dependency Injection

The package provides an extension method for DI registration:

```csharp
services.AddMessageRouter(
    mr => mr
        .UseServer()
        .UseAccessToken(MessageRouterAccessToken)
);
```

### 2. Consume in Your Application

Inject `IMessageRouter` into your services or components:

```csharp
using MorganStanley.ComposeUI.Messaging.Client;

public class MyService
{
    private readonly IMessageRouter _messageRouter;

    public MyService(IMessageRouter messageRouter)
    {
        _messageRouter = messageRouter;
    }

    // Use _messageRouter to publish, subscribe, invoke, or register services
}
```

### 3. Using the `IMessageRouter` Interface

The `IMessageRouter` interface provides advanced messaging capabilities, including:

- **Publishing messages:** Send messages to a topic for all subscribers.
- **Subscribing to topics:** Receive messages published to a specific topic.
- **Invoking services:** Send a request and receive a response from a registered service.
- **Registering services:** Expose a handler that can process requests from other clients.

#### Example: Publish and Subscribe

```csharp
// Subscribe to a topic
ValuTask HandleTopicMessage(MessageBuffer? payload)
{
    Console.WriteLine(payload.GetString());
    return ValueTask.CompletedTask;
}

await _messageRouter.SubscribeAsync(
    "exampleTopic",
    AsyncObserver.Create<TopicMessage>(x => HandleTopicMessage(x.Payload)));


// Publish a message to the topic
await _messageRouter.PublishAsync(
    'exampleTopic',
    MessageBuffer.Factory.CreateJson(payloadObject, _jsonSerializerOptions));
```

#### Example: Register and Invoke a Service

```csharp
// Register a service handler
await _messageRouter.RegisterServiceAsync(
    "exampleTopic",
    (endpoint, payload, context) =>
    {
        Console.WriteLine("Payload is handled.");
        return new ValueTask<MessageBuffer?>(MessageBuffer.Create("Hello"));
    });

// Invoke the service
var resultBuffer = await _messageRouter.InvokeAsync(
    "exampleTopic",
    MessageBuffer.Factory.CreateJson(payloadObject, _jsonSerializerOptions));

var result = resultBuffer.ReadJson<MyClass>(_jsonSerializerOptions);
```

These capabilities enable flexible, decoupled communication patterns in distributed .NET applications.


## Dependencies

- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
- [Microsoft.Extensions.Options](https://www.nuget.org/packages/Microsoft.Extensions.Options)
- [System.Reactive](https://www.nuget.org/packages/System.Reactive)
- [System.Reactive.Async](https://www.nuget.org/packages/System.Reactive.Async)
- [Nito.AsyncEx](https://www.nuget.org/packages/Nito.AsyncEx)
- [System.IO.Pipelines](https://www.nuget.org/packages/System.IO.Pipelines)

## When to Use

- When your .NET application needs to communicate with other services or components via the ComposeUI Message Router
- When you want to leverage a standardized, scalable messaging infrastructure
- When you need advanced messaging features via the `IMessageRouter` interface

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.