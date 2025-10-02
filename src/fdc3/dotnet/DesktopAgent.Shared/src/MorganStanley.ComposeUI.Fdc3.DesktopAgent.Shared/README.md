# MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared

This library contains shared components of the FDC3 DesktopAgent implementations. It is .NET Standard 2.0 library that provides shared contracts, data models, and exception types for FDC3 Desktop Agent implementations within the ComposeUI ecosystem. 
It is designed to facilitate interoperability and standardization of FDC3 messaging, context, and intent handling between desktop applications and services.

## Features
- **FDC3 Contracts**: Defines interfaces and data models for FDC3 operations, including context broadcasting, intent handling, and application management.
- **Exception Handling**: Provides custom exception types for handling FDC3-related errors and issues.
- **Cross-Platform Compatibility**: Built on .NET Standard 2.0, ensuring compatibility with a wide range of .NET implementations.
- **Extensibility**: Designed to be extended by specific DesktopAgent implementations, allowing for customization and additional functionality.
- **Context Models**: Data structures for FDC3 operations
- **Serialization support**: Utilities for serializing and deserializing FDC3 messages.
- **Interoperability**: Shared types used by both client and backend components to ensure consistent communication.

## Getting Started
To use the MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared library in your project, you can follow one of these steps:
1. **Install the NuGet Package**: You can add the library to your project via NuGet Package Manager. Use the following command in the Package Manager Console:
   ```
   Install-Package MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared
   ```
2. **Add References**: Ensure your project references the necessary dependencies, including any specific FDC3 implementations you plan to use.
   ``` bash
    dotnet add package MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared
   ```

## Example using request type
```csharp
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
var request = new IntentListenerRequest
        {
            Fdc3InstanceId = _instanceId,
            Intent = intent,
            State = SubscribeState.Subscribe
        };
```

## Dependencies
- Finos.Fdc3
- System.Text.Json

- ## Contributing
Contributions are welcome! Please submit issues or pull requests via GitHub.

## License
This library is licensed under the Apache License, Version 2.0.

## Note
This package is intended for use as a shared dependency between ComposeUI FDC3 Desktop Agent client and backend components. It does not provide a standalone FDC3 implementation.