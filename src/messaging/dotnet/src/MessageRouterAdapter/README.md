# MorganStanley.ComposeUI.MessagingAdapter

`MorganStanley.ComposeUI.MessagingAdapter` provides an implementation of the `IMessaging` abstraction that adapts the ComposeUI Message Router to a simple, dependency-injectable messaging interface. This enables seamless integration of the Message Router into .NET applications, allowing components to publish, subscribe, invoke, and register services using a consistent API.

## Description

This package acts as an adapter between the low-level `IMessageRouter` interface and the higher-level `IMessaging` abstraction. It wraps message router operations, providing consistent exception handling and a simplified API for application developers.

## Installation
> **Note:** This package is not yet published to NuGet. In the future, you will be able to install it as follows:

Install via NuGet:

```shell
dotnet package add MorganStanley.ComposeUI.Messaging.MessageRouterAdapter
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.MessageRouterAdapter
```

## Usage

### 1. Register the Adapter in Dependency Injection

Add the adapter to your DI container, ensuring that an `IMessageRouter` implementation is also registered (for example, using the ComposeUI Message Router client):

```csharp
using MorganStanley.ComposeUI.MessagingAdapter;

services.AddMessageRouter(builder =>
{
    builder.UseServer(); // or .UseWebSockets(), etc.
});

services.AddMessageRouterMessagingAdapter();
```

### 2. Consume the IMessaging Interface

Inject `IMessaging` into your services or components:

```csharp
using MorganStanley.ComposeUI.Messaging.Abstractions;

public class MyService
{
    private readonly IMessaging _messaging;

    public MyService(IMessaging messaging)
    {
        _messaging = messaging;
    }

    public async Task PublishAsync(string topic, string message)
    {
        await _messaging.PublishJsonAsync(topic, message);
    }
}
```

### 3. Registering and Invoking Services

Register a service endpoint:

```csharp
internal ValueTask<MyResponse?> MyServiceHandler(MyRequest? request)
{
    //TODO: implementation
    return new ValueTask<MyResponse?>(null);
}

_service = await _messaging.RegisterJsonServiceAsync<MyRequest, MyResponse>("myService", MyServiceHandler, _jsonSerializerOptions);
```

Invoke a service:

```csharp
string? response = await _messaging.InvokeServiceAsync("myService", JsonSerializer.Serialize(new MyRequest(), _jsonSerializerOptions));
```

### 4. Subscribing and Publishing

Subscribe to a topic:

```csharp
await _messaging.SubscribeAsync("myTopic", async message =>
{
    // Handle incoming message
    return new ValueTask();
});
```

Publish to a topic:

```csharp
await _messaging.PublishJsonAsync("myTopic", JsonSerializer.Serialize(new MyRequest(), _jsonSerializerOptions), , _jsonSerializerOptions, CancellationToken.None);
```

## Setup

- Register the `IMessaging` implementation in your DI container as shown above.
- Ensure that an `IMessageRouter` implementation is available.
- Use the `IMessaging` interface throughout your application for messaging operations.

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.