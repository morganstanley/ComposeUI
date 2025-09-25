
# ComposeUI FDC3 Desktop Agent

## Overview
This backend service delivers FDC3 Desktop Agent capabilities for FDC3-based platforms, allowing applications to register, discover, and interact with each other efficiently. It ensures data caching and offers APIs to manage FDC3 context, intents, and actions.
ComposeUI FDC3 Desktop Agent is a lightweight implementation of the FDC3 standard, designed to enable seamless interoperability between financial desktop applications. It provides standardized APIs for application communication, context sharing, and action invocation.
The project targets .NET Standard 2.0, ensuring compatibility across a broad range of .NET applications.


## Setup
Install the NuGet package:

```shell
dotnet add package MorganStanley.ComposeUI.Fdc3.DesktopAgent
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Fdc3.DesktopAgent
```

Then, you can use the FDC3 Desktop Agent in your shell by adding the following code to your startup configuration:

```csharp
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

void ConfigureFdc3(IServiceCollection services, IConfiguration configuration)
{
    var fdc3Section = configuration.GetSection("FDC3");
    services.Configure<Fdc3DesktopAgentOptions>(fdc3Section.GetSection("DesktopAgent"));
    services.AddFdc3DesktopAgent();
    // Register dependencies such as IAppDirectory, IModuleLoader, etc.
}
```

2. **Add configuration to your appsettings.json:**

```json
"FDC3": {
  "DesktopAgent": {
    "ListenerRegistrationTimeout": "00:01:00",
    "IntentResultTimeout": "00:00:30"
  }
}
```

Alternatively, you can use the provided extension method:
```csharp
void ConfigureFdc3()
{
    serviceCollection.AddFdc3DesktopAgent(builder => 
    {
        builder.Configure(options => 
        {
            options.ListenerRegistrationTimeout = TimeSpan.FromMinutes(1);
            ...
        });
    });
    ... // other FDC3 configurations
}
```

This registers the FDC3 Desktop Agent service in your application, enabling its features and allowing you to leverage the FDC3 client libraries for seamless data sharing, queries, communication between applications.

## Configuration Options

- `ChannelId`: (Optional) The ID of the user channel to create and join on startup. If set, the Desktop Agent will automatically join this channel.
- `UserChannelConfigFile`: (Optional) The URI of a static JSON file specifying the set of user channels supported by the Desktop Agent.
- `UserChannelConfig`: (Optional) An array of user channel definitions (`ChannelItem[]`) to configure available user channels directly in code or configuration.
- `IntentResultTimeout`: (Optional) Timeout for receiving intent results from the backend (default: 5 seconds).
- `ListenerRegistrationTimeout`: (Optional) Timeout for registering listeners when launching a new FDC3 app instance (default: 5 seconds).

All options can be set via the `Fdc3DesktopAgentOptions` class or in your configuration file.


## Dependencies

- [MorganStanley.ComposeUI.Messaging.Abstractions](https://www.nuget.org/packages/MorganStanley.ComposeUI.Messaging.Abstractions)
- [MorganStanley.ComposeUI.ModuleLoader.Abstractions](https://www.nuget.org/packages/MorganStanley.ComposeUI.ModuleLoader.Abstractions)
- [MorganStanley.ComposeUI.Utilities] (https://www.nuget.org/packages/MorganStanley.ComposeUI.Utilities)
- [Finos.Fdc3](https://www.nuget.org/packages/Finos.Fdc3)
- [Finos.Fdc3.AppDirectory](https://www.nuget.org/packages/Finos.Fdc3.AppDirectory)
- [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces)
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions)
- [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions)
- [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable)
- [System.Reactive.Async](https://www.nuget.org/packages/System.Reactive.Async)
- [TestableIO.System.IO.Abstractions](https://www.nuget.org/packages/TestableIO.System.IO.Abstractions)
- [TestableIO.System.IO.Abstractions.Wrappers](https://www.nuget.org/packages/TestableIO.System.IO.Abstractions.Wrappers)

## Note
To use the FDC3 Desktop Agent service, you must register the following dependencies in your `IServiceCollection`:

- `IModuleLoader`: Handles dynamic module loading and management.
- `IAppDirectory`: Provides FDC3-compliant app metadata discovery.
- `IMessaging`: Enables communication between components and services.

As defined above, the service communicates using the `IMessaging` interface. To integrate your own messaging solution, implement this interface and register it in the service collection. Alternatively, you can use the existing `MessageRouterMessaging` implementation available through the `MorganStanley.ComposeUI.Messaging.MessageRouterAdapter` NuGet package.

**Alternatively, for `IAppDirectory`, you can use the [MorganStanley.ComposeUI.Fdc3.AppDirectory](https://www.nuget.org/packages/MorganStanley.ComposeUI.Fdc3.AppDirectory) NuGet package, which provides a ready-to-use implementation.**  
Currently, we do not provide a `ModuleLoader` implementation as a NuGet package, so you will need to implement and register your own for `IModuleLoader`.


Example setup in your `Startup` or composition root:

```csharp
services.AddSingleton<IModuleLoader, YourModuleLoaderImplementation>();
services.AddSingleton<IAppDirectory, YourAppDirectoryImplementation>();
services.AddSingleton<IMessaging, YourMessagingImplementation>();

services.AddFdc3DesktopAgent();
```

These dependencies are required for the Desktop Agent to function correctly and enable features such as app discovery, intent resolution, and inter-application messaging.

## License

This project is licensed under the [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0).

&copy; Morgan Stanley. See NOTICE file for additional information.