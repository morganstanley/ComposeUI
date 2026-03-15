# MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client
MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client is a .NET library (.NET Standard 2.0) that provides a client implementation of the FDC3 Desktop Agent API for ComposeUI applications.
It enables .NET applications to interact with FDC3-compliant desktop agents, facilitating interoperability, context sharing, and intent-based communication between financial desktop applications.

## Features
- Implements the `IDesktopAgent` interface for FDC3 operations.
- Supports context and intent listeners, broadcasting, and channel management.
- Provides methods for opening applications, raising intents, and retrieving app metadata.
- Integrates with ComposeUI messaging abstraction (`IMessaging`), so it doesn't rely on any actual messaging implementation.
- Extensible logging support via `ILogger`.

## Installation
Add a reference to the NuGet package (if available) or include the project in your solution.

## Usage
Register the client in your dependency injection container:
```csharp
services.AddFdc3DesktopAgentClient();
```

Use the `IDesktopAgent` interface in your application:
```csharp
public class MyFdc3Service
{
    private readonly IDesktopAgent _desktopAgent;

    public MyFdc3Service(IDesktopAgent desktopAgent)
    {
        _desktopAgent = desktopAgent;
    }

    public async Task<IAppMetadata> GetAppMetadata(IAppIdentifier app)
    {
        await _desktopAgent.GetAppMetadata(app);
    }
}
```

## API Overview
Key methods provided by IDesktopAgent:
- AddContextListener<T>(string? contextType, ContextHandler<T> handler) where T : IContext
- AddIntentListener<T>(string intent, IntentHandler<T> handler) where T : IContext
- Broadcast(IContext context)
- CreatePrivateChannel()
- FindInstances(IAppIdentifier app)
- FindIntent(string intent, IContext? context = null, string? resultType = null)
- FindIntentsByContext(IContext context, string? resultType = null)
- GetAppMetadata(IAppIdentifier app)
- GetCurrentChannel()
- GetInfo()
- GetOrCreateChannel(string channelId)
- GetUserChannels()
- JoinUserChannel(string channelId)
- LeaveCurrentChannel()
- Open(IAppIdentifier app, IContext? context = null)
- RaiseIntent(string intent, IContext context, IAppIdentifier? app = null)
- RaiseIntentForContext(IContext context, IAppIdentifier? app = null)

## IDesktopAgentClientFactory
The `IDesktopAgentClientFactory` interface provides a shell-specific factory for resolving `IDesktopAgent` instances. This abstraction allows the Desktop Agent client to remain decoupled from the specifics of how the `IDesktopAgent` is provided, enabling greater flexibility and testability.

> **Important:** The `IDesktopAgentClientFactory` implementation should be registered as a singleton in your dependency injection container to ensure proper FDC3 integration. A single instance is required to maintain consistent state and coordination across all modules and in-process applications.

### Methods
- **GetDesktopAgentAsync(string identifier, Action\<IDesktopAgent\> onReady)**: Returns the `IDesktopAgent` implementation for a given module identifier. The `onReady` callback signals when the desktop agent client is ready.
- **RegisterInProcessAppPropertiesAsync(Fdc3Properties fdc3Properties)**: Registers FDC3 properties for in-process apps, enabling them to participate in the FDC3 ecosystem. This is particularly important for distinguishing multiple in-process apps running within the same shell.

### Example
```csharp
public class MyShellIntegration
{
    private readonly IDesktopAgentClientFactory _factory;

    public MyShellIntegration(IDesktopAgentClientFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeModule(string moduleId)
    {
        await _factory.GetDesktopAgentAsync(moduleId, desktopAgent =>
        {
            // Desktop agent is ready for use
        });
    }
}
```

## Fdc3Properties
The `Fdc3Properties` class represents the FDC3 properties for an in-process application, providing necessary identification and context information for proper FDC3 integration.

### Properties
| Property | Description |
|----------|-------------|
| `AppId` | The unique application identifier as defined in the FDC3 App Directory. |
| `InstanceId` | The unique instance identifier for this running instance of the application. |
| `ChannelId` | The channel identifier that the application is currently joined to. |
| `OpenAppContextId` | The context identifier used when opening the application, typically containing context data passed during Open or RaiseIntent operations. |

### Example
```csharp
var properties = new Fdc3Properties
{
    AppId = "my-app",
    InstanceId = Guid.NewGuid().ToString(),
    ChannelId = "fdc3.channel.1",
    OpenAppContextId = "context-123"
};

await _desktopAgentClientFactory.RegisterInProcessAppPropertiesAsync(properties);
```

## Dependencies
- Finos.Fdc3
- Microsoft.Extensions.Logging.Abstractions
- MorganStanley.ComposeUI.Messaging.Abstractions

## Contributing
Contributions are welcome! Please submit issues or pull requests via GitHub.

## License
This library is licensed under the Apache License, Version 2.0.