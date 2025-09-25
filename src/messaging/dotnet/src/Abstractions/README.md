# MorganStanley.ComposeUI.Messaging.Abstractions

`MorganStanley.ComposeUI.Messaging.Abstractions` provides interfaces and contracts for messaging in ComposeUI-based .NET applications. It enables loosely coupled communication between components and services, supporting extensibility, testability, and integration with dependency injection.

## Features

- Defines the `IMessaging` interface and related contracts for message-based communication.
- Enables decoupling of messaging logic from implementation details.
- Designed for extensibility and custom implementations.
- Compatible with .NET Standard 2.0.

## Installation

Install via NuGet:

```shell
dotnet add package MorganStanley.ComposeUI.Messaging.Abstractions
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.Abstractions
```

## Usage

1. **Implement the IMessaging Interface**

Create your own messaging implementation by implementing the `IMessaging` interface:

```csharp
using MorganStanley.ComposeUI.Messaging;

public class MyMessaging : IMessaging
{
    // Implement interface methods for sending and receiving messages
}
```

2. **Register with Dependency Injection**

Register your implementation in the DI container:

```csharp
services.AddSingleton<IMessaging, MyMessaging>();
```

3. **Consume in Your Application**

Inject `IMessaging` where needed:

```csharp
public class MyService
{
    private readonly IMessaging _messaging;

    public MyService(IMessaging messaging)
    {
        _messaging = messaging;
    }

    // Use _messaging to send or receive messages
}
```

## When to Use

- When building libraries or applications that need to interact with messaging without depending on a specific implementation.
- When you want to enable testability and flexibility by abstracting messaging logic.

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.