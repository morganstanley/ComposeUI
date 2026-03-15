# MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client
MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client is a .NET library (.NET Standard 2.0) that provides a client implementation of the FDC3 Desktop Agent API for ComposeUI applications.
It enables .NET applications to interact with FDC3-compliant desktop agents, facilitating interoperability, context sharing, and intent-based communication between financial desktop applications.

## Features
- Implements the `IDesktopAgent` interface from the `Finos.Fdc3` NuGet package for FDC3 operations.
- Supports context and intent listeners, broadcasting, and channel management.
- Provides methods for opening applications, raising intents, and retrieving app metadata.
- Integrates with ComposeUI messaging abstraction (`IMessaging`), so it doesn't rely on any actual messaging implementation.
- Extensible logging support via `ILogger`.


## Target Framework
- .NET Standard 2.0

Compatible with .NET (Core), .NET Framework.


## Dependencies
- [Finos.Fdc3](https://www.nuget.org/packages/Finos.Fdc3)
- [Finos.Fdc3.AppDirectory](https://www.nuget.org/packages/Finos.Fdc3.AppDirectory)
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)
- [MorganStanley.ComposeUI.Messaging.Abstractions](https://www.nuget.org/packages/MorganStanley.ComposeUI.Messaging.Abstractions)
- [MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared](https://www.nuget.org/packages/MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared)


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
    private IChannel? _currentChannel;
    
    public MyFdc3Service(IDesktopAgent desktopAgent)
    {
        _desktopAgent = desktopAgent;
    }

    public async Task<IAppMetadata> GetAppMetadata(IAppIdentifier app)
    {
        await _desktopAgent.GetAppMetadata(app);
    }
    
    public async Task SendContext(IContext context)
    {
        if (_currentChannel == null)
        {
            //Other possibility to broadcast on a channel is:
            var channel = await _desktopAgent.JoinUserChannel("fdc3.channel.1");
            await channel.Broadcast(context);
            return;
        }

        await _desktopAgent.Broadcast(context);
    }
    ...
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

## Dependencies
- Finos.Fdc3
- Microsoft.Extensions.Logging.Abstractions
- MorganStanley.ComposeUI.Messaging.Abstractions

## IDesktopAgentClientFactory
The `IDesktopAgentClientFactory` interface provides a shell-specific factory for resolving `IDesktopAgent` instances. This abstraction allows the Desktop Agent client to remain decoupled from the specifics of how the `IDesktopAgent` is provided, enabling greater flexibility and testability.

> **Important:** The `IDesktopAgentClientFactory` implementation should be registered as a singleton in your dependency injection container to ensure proper FDC3 integration. A single instance is required to maintain consistent state and coordination across all modules and in-process applications.

### Methods
- **GetDesktopAgentAsync(string identifier, Action\<IDesktopAgent\> onReady)**: Returns the `IDesktopAgent` implementation for a given module identifier. The `onReady` callback signals when the desktop agent client is ready.
- **RegisterInProcessAppPropertiesAsync(Fdc3StartupProperties fdc3Properties)**: Registers FDC3 properties for in-process apps, enabling them to participate in the FDC3 ecosystem. This is particularly important for distinguishing multiple in-process apps running within the same shell.

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

## Fdc3StartupProperties
The `Fdc3StartupProperties` class represents the FDC3 properties for an application, providing necessary identification and context information for proper FDC3 integration.

### Properties
| Property | Description |
|----------|-------------|
| `AppId` | The unique application identifier as defined in the FDC3 App Directory. |
| `InstanceId` | The unique instance identifier for this running instance of the application. |
| `ChannelId` | The channel identifier that the application is currently joined to. |
| `OpenedAppContextId` | The context identifier used when opening the application, typically containing context data passed during Open or RaiseIntent operations. |

### Example
```csharp
var properties = new Fdc3StartupProperties
{
    AppId = "my-app",
    InstanceId = Guid.NewGuid().ToString(),
    ChannelId = "fdc3.channel.1",
    OpenedAppContextId = "context-123"
};

await _desktopAgentClientFactory.RegisterInProcessAppPropertiesAsync(properties);
```

### Channel Selector Behavior for Native Clients
Native clients must handle channel selection UI independently since the Desktop Agent Client does not have knowledge of the container's UI. The implementation uses a messaging-based approach where containers can register their own channel selector logic.

#### Registering a Channel Selector
Each container instance should register its own channel selector logic by subscribing to the `Fdc3Topic.ChannelSelectorFromAPI({instanceId})` topic. This service is invoked whenever `JoinUserChannel` is called from the API, allowing the UI to update and reflect the currently joined user channel.

#### Initial User Channel Behavior
When the Desktop Agent Client is initialized with an initial user channel (via `Fdc3StartupProperties.ChannelId` or environment variable), the implementation joins the channel **without triggering the registered channel selector service**. This is intentional because:
- The container already knows the initial channel value (it provided it during startup)
- This avoids unnecessary round-trips and potential UI flickering during initialization
- The container can immediately display the correct initial state without waiting for a callback

The container is responsible for displaying the initial user channel state in its UI channel selector component.

#### JoinUserChannel Triggers the Channel Selector
After initialization, whenever `JoinUserChannel(channelId)` is called (either by the application or via the UI), the implementation:
1. Joins the specified user channel
2. Triggers the registered channel selector service at `Fdc3Topic.ChannelSelectorFromAPI({instanceId})` with the channel ID

This ensures the UI can update to reflect the new channel membership.

#### Channel Selection from UI
If you provide a UI channel selector component, it can trigger channel joins by invoking the `Fdc3Topic.ChannelSelectorFromUI({instanceId})` service with a `JoinUserChannelRequest` payload. The Desktop Agent Client registers this service during initialization and will call `JoinUserChannel` internally when invoked.

```csharp
// Example: Container registering to receive channel selector updates
await messaging.RegisterServiceAsync(
    Fdc3Topic.ChannelSelectorFromAPI(instanceId),
    (channelId) =>
    {
        // Update your UI channel selector to display the joined channel
        UpdateChannelSelectorUI(channelId);
        return ValueTask.FromResult<string?>(null);
    });

// Example: UI triggering a channel join
var request = new JoinUserChannelRequest { ChannelId = "fdc3.channel.1" };
await messaging.InvokeServiceAsync(
    Fdc3Topic.ChannelSelectorFromUI(instanceId),
    JsonSerializer.Serialize(request));
```

> **Note:** The initial user channel provided during startup will not trigger the channel selector callback. The container must handle displaying the initial channel state itself based on the `Fdc3StartupProperties.ChannelId` value it provided.

### Getting or Creating a App Channel
You can get or create an app channel by using:
```csharp
var appChannel = await desktopAgent.GetOrCreateChannel("your-app-channel-id");
var listener = await appChannel.AddContextListener("fdc3.instrument", (ctx, ctxMetadata) =>
{
    Console.WriteLine($"Received context on app channel: {ctx}");
});

//Initiator shouldn't receive back the broadcasted context
await appChannel.Broadcast(context);
```


## Default Usage of IDesktopAgent
### Broadcasting Context
An app is not able to broadcast any message unless it has joined a channel. After joining a channel, you can broadcast context to all listeners in that channel.
```csharp
var context = new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name");
await desktopAgent.Broadcast(context);
```

Or you can broadcast context using the `IChannel` API:
```csharp
await desktopAgent.JoinUserChannel("fdc3.channel.2");
var channel = await desktopAgent.GetCurrentChannel();
await channel.Broadcast(context);
```

### Adding Context Listener
You may register context listeners either before or after joining a channel; however, listeners will not receive any context unless the DesktopAgent has joined a channel.
When an application is launched using the fdc3.open call with a context, the appropriate listener in the opened app will be invoked with that context before any other context messages are delivered.
```csharp
var listener = await desktopAgent.AddContextListener("fdc3.instrument", (ctx, ctxMetadata) =>
{
    Console.WriteLine($"Received context: {ctx}");
});
```

### Joining to a User Channel
Based on the standard you should join to a channel before broadcasting context (from top-level) to it or before you can receive the messages after you have added your top-level context listener. If an app is joined to a channel every top-level already registered context listener will call its handler with the latest context on that channel.
```csharp
var channel = await desktopAgent.JoinUserChannel("fdc3.channel.1");
```

### Leaving a Channel
You can leave the current channel by calling the LeaveCurrentChannel method. After leaving a channel, the app will not receive any context messages until it joins another channel.
```csharp
await desktopAgent.LeaveCurrentChannel();
```

### Getting the Current Channel
You can get the current channel using the GetCurrentChannel method. If the app is not joined to any channel, it will return null.
```csharp
var currentChannel = await desktopAgent.GetCurrentChannel();
```

### Getting Info about the current App
You can get the metadata of the current app using the GetInfo method.
```csharp
var implementationMetadata = await desktopAgent.GetInfo();
```

### Getting Metadata of an App
You can get the metadata of an app using the GetAppMetadata method by providing the AppIdentifier.
```csharp
var appMetadata = await desktopAgent.GetAppMetadata(new AppIdentifier("your-app-id", "your-instance-id"));
```

### Getting User Channels
You can retrieve user channels by using:
```csharp
var channels = await desktopAgent.GetUserChannels();
await desktopAgent.JoinUserChannel(channels[0].Id);
```

### Finding apps based on the specified intent
You can find/search for applications from the AppDirectory by using the `FindIntent` function:
```csharp
var apps = await desktopAgent.FindIntent("ViewChart", new Instrument(new InstrumentID { Ticker = "AAPL" }), "expected_resultType"));
```

### Finding instances for the specified app
You can find the currently FDC3 enabled instances for the specified app by using the `FindInstances` function:
```csharp
var instances = await desktopAgent.FindInstances("your-app-id");
```

### Finding intents by context
You can find the apps that can handle for the specified context by using the `FindIntentsByContext` function:
```csharp
var context = new Instrument(new InstrumentID { Ticker = "AAPL" }, "Apple Inc.");
var appIntents = await desktopAgent.FindIntentsByContext(context);
```

### Raising intent for context
You can raise an intent for the specified context by using the `RaiseIntentForContext` function and return its result by using the `GetResult` of the returned `IIntentResolution`:
```csharp
var intentResolution = await desktopAgent.RaiseIntentForContext(context, appIdentifier);
var intentResult = await intentResolution.GetResult();
```

### Adding Intent Listener
You can register an intent listener by using the `AddIntentListener` function:
```csharp
var currentChannel = await desktopAgent.GetCurrentChannel();
var listener = await desktopAgent.AddIntentListener<Instrument>("ViewChart", async (ctx, ctxMetadata) =>
{
    Console.WriteLine($"Received intent with context: {ctx}");
    return currentChannel;
});
```

### Raising intents
You can raise an intent by using the `RaiseIntent` function and return its result by using the `GetResult` of the returned `IIntentResolution`:
```csharp
var intentResolution = await desktopAgent.RaiseIntent("ViewChart", context, appIdentifier);
var intentResult = await intentResolution.GetResult();
```

### Opening an app
You can open an app by using the `Open` function:
```csharp
var appIdentifier = new AppIdentifier("your-app-id");
var instrument = new Instrument();

var appInstance = await desktopAgent.Open(appIdentifier, instrument);
//The opened app should handle the context if it has registered a listener for that context type; if it does not register its context listener in time the open call will fail
```

### Creating Private Channel
You can create a private channel by using the `CreatePrivateChannel` function:
```csharp
var privateChannel = await desktopAgent.CreatePrivateChannel("your-private-channel-id");
var contextListenerHandler = privateChannel.OnAddContextListener((ctx) => {
    Console.WriteLine($"Private channel context listener has been added for context: {ctx}");
});

var unsubscribeHandler = privateChannel.OnUnsubscribe((ctx) => {
    Console.WriteLine($"Private channel context listener has been unsubscribed for context: {ctx}");
});

var disconnectHandler = privateChannel.OnDisconnect(() => {
    Console.WriteLine("Private channel has been disconnected");
});
```

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.

## Contributing
Contributions are welcome! Please submit issues or pull requests via GitHub.
