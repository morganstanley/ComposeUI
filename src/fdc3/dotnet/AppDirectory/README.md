# MorganStanley.ComposeUI.Fdc3.AppDirectory

`MorganStanley.ComposeUI.Fdc3.AppDirectory` is a .NET Standard 2.0 library that implements the [FDC3 App Directory](https://fdc3.finos.org/docs/app-directory/overview) standard. It enables .NET applications to discover and retrieve FDC3-compliant app metadata from files or HTTP endpoints, with built-in caching and extensibility.

## Features

- Retrieve FDC3 app metadata from file or HTTP sources
- Pluggable caching with configurable expiration
- Extensible host manifest mapping
- File system change detection for live reloads
- Easy integration with ASP.NET Core and dependency injection

## Installation

Install the NuGet package:

```shell
dotnet package add MorganStanley.ComposeUI.Fdc3.AppDirectory
```

Or via the NuGet Package Manager:

``` shell
PM> Install-Package MorganStanley.ComposeUI.Fdc3.AppDirectory
```

## Usage

### 1. Register the AppDirectory service

In your application's startup, register the service with dependency injection:

```csharp
void ConfigureFdc3()
{
    var fdc3ConfigurationSection = context.Configuration.GetSection("FDC3");
    var fdc3Options = fdc3ConfigurationSection.Get<Fdc3Options>(); // Your defined options class containing the `AppDirectoryOptions`

    if (fdc3Options is { EnableFdc3: true })
    {
        services.AddFdc3AppDirectory();
        services.Configure<Fdc3Options>(fdc3ConfigurationSection);
        services.Configure<AppDirectoryOptions>(fdc3ConfigurationSection.GetSection(nameof(fdc3Options.AppDirectory)));
        ... // Other configuration
    }
}
```

Alternatively, use the provided extension method:

```csharp
services.AddFdc3AppDirectory(options =>
{
    options.Source = new Uri("file:///path/to/apps.json");
});
```

### 2. Retrieve FDC3 Apps

Inject `IAppDirectory` where needed:

```csharp
public class MyService
{
    private readonly IAppDirectory _appDirectory;

    public MyService(IAppDirectory appDirectory)
    {
        _appDirectory = appDirectory;
    }

    public async Task ListAppsAsync()
    {
        var apps = await _appDirectory.GetApps();
        foreach (var app in apps)
        {
            Console.WriteLine(app.Name);
        }
    }
}
```

### 3. Configuration Options

- `Source`: (Optional) URI pointing to the app directory JSON file or HTTP endpoint. If not set, an empty app array will be returned and cached to retrieve data from.
- `CacheExpirationInSeconds`: (Optional) Duration (in seconds) to cache app metadata. Defaults to 1 hour.
- `HttpClientName`: (Optional) Name of the HTTP client for custom configuration. If specified, an `IHttpClientFactory` must be provided to the `AppDirectory` constructor with a matching client. If an HTTP endpoint is set as the source and no `HttpClientName` is provided, a new `HttpClient` instance will be used to fetch data.


### 4. Supported Sources

- **File**: `file:///path/to/apps.json`
- **HTTP/HTTPS**: `https://directory.fdc3.finos.org/v2/apps/`

## Example

See [examples/fdc3-appdirectory/README.md](../../../../../examples/fdc3-appdirectory/apps.json) for a sample JSON app directory. For usage you can configure the shell to get the data.

## License

This project is licensed under the [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0).

&copy; Morgan Stanley. See NOTICE file for additional information.