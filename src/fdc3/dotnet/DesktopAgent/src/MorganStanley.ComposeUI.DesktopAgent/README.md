
# ComposeUI FDC3 Desktop Agent

## Overview
This backend service delivers FDC3 Desktop Agent capabilities for FDC3-based platforms, allowing applications to register, discover, and interact with each other efficiently. It ensures data caching and offers APIs to manage FDC3 context, intents, and actions.
ComposeUI FDC3 Desktop Agent is a lightweight implementation of the FDC3 standard, designed to enable seamless interoperability between financial desktop applications. It provides standardized APIs for application communication, context sharing, and action invocation.
The project targets .NET Standard 2.0, ensuring compatibility across a broad range of .NET applications.


## Setup
First, install the required dependencies:
```bash
dotnet add package MorganStanley.ComposeUI.Fdc3.DesktopAgent --version <version>
```

Then, you can use the FDC3 Desktop Agent in your shell by adding the following code to your startup configuration:

```csharp
void ConfigureFdc3()
{
    var fdc3ConfigurationSection = context.Configuration.GetSection("FDC3");
    var fdc3Options = fdc3ConfigurationSection.Get<Fdc3Options>();

    if (fdc3Options is { EnableFdc3: true })
    {
         serviceCollection.AddFdc3DesktopAgent();
         ... // other FDC3 configurations
    }
}
```

This registers the FDC3 Desktop Agent service in your application, enabling its features and allowing you to leverage the FDC3 client libraries for seamless data sharing, queries, communication between applications.