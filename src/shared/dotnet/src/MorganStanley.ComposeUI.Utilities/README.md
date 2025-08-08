# MorganStanley.ComposeUI.Utilities

`MorganStanley.ComposeUI.Utilities` provides helper classes and extensions to support ComposeUI-based .NET applications. These utilities are designed to simplify common tasks and enhance productivity when building applications with ComposeUI.
The library targets .NET Standard 2.0.

## Description

This package contains reusable utilities, extension methods, and helper classes that can be used across different ComposeUI modules and services. It targets .NET Standard 2.0 for broad compatibility.

## Installation

Install via NuGet:

```shell
dotnet package add MorganStanley.ComposeUI.Utilities
```

Or via the NuGet Package Manager:

```
PM> Install-Package MorganStanley.ComposeUI.Utilities
```

## Dependencies

This package depends on the following NuGet packages:

- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine)
- [System.ComponentModel.Annotations](https://www.nuget.org/packages/System.ComponentModel.Annotations)

## Usage

Add a reference to the package in your project and use the provided utilities as needed. Example usage will depend on the specific helpers included in the package.

### Example: Reading an Embedded Resource

You can use the `ResourceReader.ReadResource` method to read the contents of an embedded resource as a string:

```csharp
using MorganStanley.ComposeUI.Utilities;

string content = ResourceReader.ReadResource("Your.Namespace.ResourceFile.txt");
```

- The `resourcePath` parameter should be the fully qualified name of the embedded resource in your assembly.
- If the resource is not found, an `InvalidOperationException` is thrown.
- This is useful for loading configuration files, templates, or other resources embedded in your project.

### Example: Deleting a GDI Object

You can use the `NativeMethods.DeleteObject` method to release system resources associated with GDI objects (such as pens, brushes, fonts, bitmaps, regions, or palettes):

```csharp
using MorganStanley.ComposeUI.Utilities;
using System;

IntPtr gdiObjectHandle = /* obtain handle from GDI operation */;
bool success = NativeMethods.DeleteObject(gdiObjectHandle);

if (!success)
{
    // Handle error, optionally using Marshal.GetLastWin32Error()
}
```

### Example: Parsing Command Line Arguments

You can use the `CommandLineParser` utility to parse strongly typed objects from command line arguments:

```csharp
using MorganStanley.ComposeUI.Utilities;
using System.ComponentModel.DataAnnotations;

public class Options
{
    [Display(Description = "The input file path")]
    public string Input { get; set; }

    [Display(Description = "The output file path")]
    public string Output { get; set; }
}

string[] args = new[] { "--input", "in.txt", "--output", "out.txt" };
Options options = CommandLineParser.Parse<Options>(args);

// Or use TryParse:
if (CommandLineParser.TryParse(args, out Options parsedOptions))
{
    // Use parsedOptions
}
```
- Properties can be annotated with `[Display(Description = "...")]` for help text.
- Property names are converted to camelCase for option names by default.

## Setup

- Add the NuGet package to your project.
- Import the relevant namespaces in your code files.
- Use the utilities and extensions to simplify your ComposeUI application development.

## Documentation

For more details, see the [ComposeUI documentation](https://morganstanley.github.io/ComposeUI/).

&copy; Morgan Stanley. See NOTICE file for additional information.