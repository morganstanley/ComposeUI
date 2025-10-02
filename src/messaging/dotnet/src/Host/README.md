# MorganStanley.ComposeUI.Messaging.Host

`MorganStanley.ComposeUI.Messaging.Host` provides a .NET host implementation for ComposeUI's Message Router. It enables hosting, managing, and routing messages between distributed .NET applications and services, integrating seamlessly with the ComposeUI messaging infrastructure for scalable and reliable communication.

## Features

- Host and manage a ComposeUI Message Router within your .NET application
- Route messages between multiple distributed clients and services
- Integrates with dependency injection and Microsoft.Extensions
- Supports in-process and out-of-process hosting scenarios
- Designed for scalability and reliability
- Targets .NET 8.0

## Installation

Install via NuGet:

```shell
dotnet add package MorganStanley.ComposeUI.Messaging.Host
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.Host
```

## Usage

### 1. Register the Message Router Host

Add the Message Router server to your DI container:

```csharp
using MorganStanley.ComposeUI.Messaging.Host;

services.AddMessageRouterServer(
    mr => mr
    .UseWebSockets(builder => {
        builder.Port = 1123;
        // Other configuration
    })
    .UseAccessTokenValidator(
        (clientId, token) =>
        {
            if (MessageRouterAccessToken != token)
                throw new InvalidOperationException("The provided access token is invalid");
        }));
```

### 2. In-Process Client Integration

To connect a client to the in-process server, use the provided extension method:

```csharp
using MorganStanley.ComposeUI.Messaging.Client;

services.AddMessageRouter(builder =>
{
    builder.UseServer(); // Connects to the in-process server
});
```

### 3. Routing and Communication

Clients registered with the in-process or external server can publish, subscribe, invoke, and register services using the `IMessageRouter` interface.


## Dependencies

- [MorganStanley.ComposeUI.Messaging.Core](https://www.nuget.org/packages/MorganStanley.ComposeUI.Messaging.Core)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

## When to Use

- When you need to host a ComposeUI Message Router in your .NET application or service
- When you want to enable scalable, decoupled messaging between distributed .NET components

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.