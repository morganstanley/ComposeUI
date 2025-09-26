# MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client
MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client is a .NET library (.NET Standard 2.0) that provides a client implementation of the FDC3 Desktop Agent API for ComposeUI applications.
It enables .NET applications to interact with FDC3-compliant desktop agents, facilitating interoperability, context sharing, and intent-based communication between financial desktop applications.

## Features
- Implements the `IDesktopAgent` interface from the `Finos.Fdc3` NuGet package for FDC3 operations.
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

## Usage
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

### Finding apps based on the specified intent
You can find/search for applications from the AppDirectory by using the `FindIntent` function:
```csharp
var apps = await desktopAgent.FindIntent("ViewChart", new Instrument(new InstrumentID { Ticker = "AAPL" }), "expected_resultType"));
```

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.

## Contributing
Contributions are welcome! Please submit issues or pull requests via GitHub.
