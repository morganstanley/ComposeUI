# MorganStanley.ComposeUI.Messaging.Core

`MorganStanley.ComposeUI.Messaging.Core` provides the core infrastructure and utilities for the ComposeUI Message Router. It supplies essential building blocks, serialization, and concurrency support for messaging components and services. This package is primarily intended for internal use by other ComposeUI messaging packages and advanced users who need low-level messaging primitives.

## Features

- Core message serialization and deserialization utilities
- Concurrency and synchronization primitives for messaging scenarios
- Shared infrastructure for building messaging clients, hosts, and adapters
- Designed for extensibility and high performance
- Compatible with .NET 8.0

## Installation

Install via NuGet:

```shell
dotnet package add MorganStanley.ComposeUI.Messaging.Core
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Messaging.Core
```

## Usage

This package is typically used as a dependency by other ComposeUI messaging packages (such as Client, Host, and Adapters). Direct usage is intended for advanced scenarios where you need to build custom messaging components or extend the ComposeUI messaging infrastructure.

**Example:**  
If you are developing a custom messaging client or host, reference this package to access serialization, concurrency, and protocol utilities.

```csharp
using MorganStanley.ComposeUI.Messaging.Core;
// Use core utilities for message handling, serialization, etc.
```

## Dependencies

- [CommunityToolkit.HighPerformance](https://www.nuget.org/packages/CommunityToolkit.HighPerformance)
- [Nito.AsyncEx.Coordination](https://www.nuget.org/packages/Nito.AsyncEx.Coordination)
- [System.Reactive](https://www.nuget.org/packages/System.Reactive)
- [System.Reactive.Async](https://www.nuget.org/packages/System.Reactive.Async)
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json)
- [MorganStanley.ComposeUI.Messaging.Abstractions](https://www.nuget.org/packages/MorganStanley.ComposeUI.Messaging.Abstractions)

## When to Use

- When building or extending ComposeUI messaging clients, hosts, or adapters
- When you need access to low-level messaging utilities and infrastructure

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.