# MorganStanley.ComposeUI.Messaging.Server

`MorganStanley.ComposeUI.Messaging.Server` is a .NET server package for ComposeUI's Message Router. It enables hosting and managing message routing infrastructure for distributed .NET applications, providing dependency injection support, extensibility, and seamless integration with ComposeUI messaging clients.

## Description

This package allows you to host a ComposeUI Message Router within your .NET application or service. It is designed for scenarios where you need to route messages between distributed components, microservices, or clients using the ComposeUI messaging infrastructure.

## Installation

Install via NuGet:

```shell
dotnet add package MorganStanley.ComposeUI.Messaging.Server
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.Server
```

## Usage

### 1. Register the Message Router Server

Add the server to your dependency injection container (e.g., in your `Program.cs`):

```csharp
using MorganStanley.ComposeUI.Messaging.Server;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMessageRouterServer(options =>
        {
            options.UseWebSockets(
                opt =>
                {
                    opt.RootPath = _webSocketUri.AbsolutePath;
                    opt.Port = _webSocketUri.Port;
                });
        });
    });

var app = builder.Build();
await app.RunAsync();
```

## Dependencies

This package depends on the following NuGet packages:

- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.Hosting.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Abstractions)
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
- [Microsoft.Extensions.Options](https://www.nuget.org/packages/Microsoft.Extensions.Options)
- [System.IO.Pipelines](https://www.nuget.org/packages/System.IO.Pipelines)
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)


### 2. Configure Routing and Services

You can configure endpoints, authentication, and other options using the provided configuration APIs.

### 3. Integrate with Clients

Clients can connect to your hosted message router using the ComposeUI messaging client libraries.

## Setup

- Register the message router server in your DI container.
- Configure server options as needed for your environment.
- Start your .NET application to host the message router.

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.