# MorganStanley.ComposeUI.ModuleLoader.Abstractions

`MorganStanley.ComposeUI.ModuleLoader.Abstractions` provides interfaces and abstractions for module loading and management in .NET applications. 
It enables dynamic discovery, loading, starting, stopping, and monitoring of modules, supporting extensibility and integration with dependency injection. This package is compatible with .NET Standard 2.0.

## Features

- Abstractions for module lifecycle management (start, stop, monitor)
- Interfaces for dynamic module discovery and loading
- Designed for extensibility and custom implementations
- Integration with dependency injection frameworks
- Suitable for ComposeUI-based and general .NET applications

## Installation

Install via NuGet:

```shell
dotnet add package MorganStanley.ComposeUI.ModuleLoader.Abstractions
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.ModuleLoader.Abstractions
```

## Dependencies

This package depends on the following NuGet packages:

- [System.Runtime](https://www.nuget.org/packages/System.Runtime)
- [System.Collections](https://www.nuget.org/packages/System.Collections)
- [System.Threading.Tasks](https://www.nuget.org/packages/System.Threading.Tasks)


## Usage

### 1. Implement the Interfaces

To use the abstractions, implement the provided interfaces in your application or library. For example:

```csharp
using MorganStanley.ComposeUI.ModuleLoader;

public class ModuleInstance : IModuleInstance
{
    public Guid InstanceId => throw new NotImplementedException();

    public IModuleManifest Manifest => throw new NotImplementedException();

    public StartRequest StartRequest => throw new NotImplementedException();

    public IEnumerable<object> GetProperties()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<T> GetProperties<T>()
    {
        throw new NotImplementedException();
    }
}
```
You can implement your own module loader by using the abstractions:

```csharp
public class MyModuleLoader : IModuleLoader
{
    public Task<IModuleInstance> StartModule(StartRequest startRequest)
    {
        // Custom logic to discover and load modules
    }

    public Task StopModule(StopRequest stopRequest)
    {
        // Custom logic to unload modules
    }

    public IObservable<LifetimeEvent> LifetimeEvents => throw new NotImplementedException();
}
```

You would need to provide at least `IModuleManifest`, `IModuleCatalog`, `IModuleInstance`, `IModuleRunner` and a `IModuleLoader` implementation with your logic.


##### Alternative: MorganStanley.ComposeUI.ModuleLoader

Alternatively, you can use the `MorganStanley.ComposeUI.ModuleLoader` package, which provides a default implementation of these abstractions for ComposeUI-based .NET applications. This package offers ready-to-use services for dynamic module discovery, loading, lifecycle management, and monitoring, all integrated with dependency injection.

###### About MorganStanley.ComposeUI.ModuleLoader

- Implements the abstractions defined in `MorganStanley.ComposeUI.ModuleLoader.Abstractions`
- Supports starting, stopping, and monitoring modules at runtime
- Integrates with .NET dependency injection for easy setup
- Provides extensibility points for custom module loading strategies
- Targets .NET 8.0

###### Setup
Please NOTE that currently we are not releasing the `MorganStanley.ComposeUI.ModuleLoader` NuGet package.

###### Usage
Currently, the package can only be tried by cloning the repository and using it with our POC Shell implementation.

In the future, you will be able to register the module loader directly in the DI container with:

```csharp
services.AddModuleLoader();
```

You can then inject and use `IModuleLoader` and related services in your application to manage modules dynamically.


### 3. Dependency Injection

Register your implementations with the DI container:

```csharp
services.AddSingleton<IModuleLoader, MyModuleLoader>();
//... Register your runner implementations
```

### 4. Monitoring and Lifecycle

Use the abstractions to monitor and control module lifecycles throughout your application.

## When to Use

- When you need a standardized way to manage modules in a .NET or ComposeUI-based application
- When you want to support dynamic loading/unloading of application components
- When you need to decouple module implementations from their consumers

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.