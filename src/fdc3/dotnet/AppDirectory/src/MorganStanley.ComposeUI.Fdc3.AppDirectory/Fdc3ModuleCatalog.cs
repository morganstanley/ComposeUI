/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using MorganStanley.ComposeUI.ModuleLoader;
using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public sealed class Fdc3ModuleCatalog : IModuleCatalog
{
    private readonly IAppDirectory _appDirectory;
    private readonly IHostManifestMapper? _hostManifestMapper;
    private readonly AppDirectoryOptions _options;
    private readonly NativeModuleLaunchOptions _launchOptions;
    private readonly ILogger<Fdc3ModuleCatalog> _logger;

    public Fdc3ModuleCatalog(
        IAppDirectory fdc3AppDirectory,
        IHostManifestMapper? hostManifestMapper = null,
        IOptions<AppDirectoryOptions>? options = null,
        NativeModuleLaunchOptions? launchOptions = null,
        ILogger<Fdc3ModuleCatalog>? logger = null)
    {
        _appDirectory = fdc3AppDirectory;
        _hostManifestMapper = hostManifestMapper;
        _options = options?.Value ?? new AppDirectoryOptions();
        _launchOptions = launchOptions ?? new NativeModuleLaunchOptions();
        _logger = logger ?? NullLogger<Fdc3ModuleCatalog>.Instance;
    }

    public async Task<IModuleManifest> GetManifest(string moduleId)
    {
        var app = await _appDirectory.GetApp(moduleId);
        return GetManifest(app);
    }

    private IModuleManifest GetManifest(Fdc3App app) => app.Type switch
    {
        AppType.Web => new Fdc3WebModuleManifest(app, _hostManifestMapper),
        AppType.Native => GetNativeManifest(app),
        _ => throw new NotSupportedException($"Unsupported module type: {app.Type}"),
    };

    private IModuleManifest GetNativeManifest(Fdc3App app)
    {
        try
        {
            return new Fdc3NativeModuleManifest(app, _hostManifestMapper, _options, _launchOptions);
        }
        catch (UriFormatException e)
        {
            // A path like \\?\C:\app.exe is not a valid uri. It must be reported like the other paths that are rejected.
            throw new NotSupportedException($"The native app '{app.AppId}' cannot be started. {e.Message}", e);
        }
    }

    public async Task<IEnumerable<string>> GetModuleIds()
    {
        var apps = await _appDirectory.GetApps();
        return apps.Select(x => x.AppId);
    }

    private class Fdc3WebModuleManifest : IModuleManifest<WebManifestDetails>
    {
        private readonly IHostManifestMapper? _hostManifestMapper;

        public Fdc3WebModuleManifest(
            Fdc3App app, 
            IHostManifestMapper? hostManifestMapper = null)
        {
            if (app.Type != AppType.Web)
            {
                throw new ArgumentException("The provided app is not a web app.", nameof(app));
            }

            _hostManifestMapper = hostManifestMapper;

            Id = app.AppId;
            Name = app.Name ?? app.Title;

            Tags = app.Categories?.ToArray() ?? [];
            AdditionalProperties = [];

            var details = _hostManifestMapper?.MapModuleDetails(app);

            if (details is WebManifestDetails webManifestDetails && webManifestDetails != default)
            {
                Details = webManifestDetails;
            }
            else
            {
                var iconSrc = app.Icons?.FirstOrDefault()?.Src;
                var url = new Uri(((WebAppDetails) app.Details).Url, UriKind.Absolute);

                Details = new WebManifestDetails
                {
                    Url = url,
                    IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                };
            }
        }

        public WebManifestDetails Details { get; init; }

        public string Id { get; init; }

        public string Name { get; init; }

        public string ModuleType => ModuleLoader.ModuleType.Web;

        public string[] Tags { get; init; }

        public Dictionary<string, string> AdditionalProperties { get; init; }
    }

    public async Task<IEnumerable<IModuleManifest>> GetAllManifests()
    {
        var apps = await _appDirectory.GetApps();
        var manifests = new List<IModuleManifest>();

        foreach (var app in apps)
        {
            try
            {
                manifests.Add(GetManifest(app));
            }
            catch (NotSupportedException e)
            {
                // One app that cannot be started must not hide the other apps of the directory.
                _logger.LogWarning(e, "The app '{AppId}' of the app directory is skipped. {Reason}", app.AppId, e.Message);
            }
        }

        return manifests;
    }


    private class Fdc3NativeModuleManifest : IModuleManifest<NativeManifestDetails>
    {
        private readonly IHostManifestMapper? _hostManifestMapper;

        public Fdc3NativeModuleManifest(
            Fdc3App app,
            IHostManifestMapper? hostManifestMapper,
            AppDirectoryOptions options,
            NativeModuleLaunchOptions launchOptions)
        {
            if (app.Type != AppType.Native)
            {
                throw new ArgumentException("The provided app is not a native app.", nameof(app));
            }

            _hostManifestMapper = hostManifestMapper;

            Id = app.AppId;
            Name = app.Name ?? app.Title;

            Tags = app.Categories?.ToArray() ?? [];
            AdditionalProperties = [];

            var details = _hostManifestMapper?.MapModuleDetails(app);

            if (details is NativeManifestDetails nativeManifestDetails)
            {
                Details = nativeManifestDetails;
            }
            else
            {
                var iconSrc = app.Icons?.FirstOrDefault()?.Src;

                var nativeDetails = (NativeAppDetails) app.Details;

                Details = new NativeManifestDetails
                {
                    Path = new Uri(nativeDetails.Path, UriKind.RelativeOrAbsolute),
                    Icon = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                };
            }

            if (Details.Arguments.Length > 0 && !options.AllowNativeArguments)
            {
                throw new NotSupportedException($"The native app '{app.AppId}' cannot be started. The app directory gives arguments, which are not allowed. Set {nameof(AppDirectoryOptions.AllowNativeArguments)} to allow them.");
            }

            if (Details.EnvironmentVariables.Count > 0 && !options.AllowNativeEnvironmentVariables)
            {
                throw new NotSupportedException($"The native app '{app.AppId}' cannot be started. The app directory gives environment variables, which are not allowed. Set {nameof(AppDirectoryOptions.AllowNativeEnvironmentVariables)} to allow them.");
            }

            Details.Path = GetValidatedPath(app.AppId, Details.Path, launchOptions);
        }

        private static Uri GetValidatedPath(string appId, Uri path, NativeModuleLaunchOptions launchOptions)
        {
            var errors = new List<string>();

            foreach (var root in launchOptions.GetEffectiveRoots())
            {
                if (NativePathValidator.TryValidate(path, root, out var fullPath, out var error))
                {
                    return NativePathValidator.ToFileUri(fullPath);
                }

                errors.Add(error);
            }

            throw new NotSupportedException($"The native app '{appId}' cannot be started. {string.Join(" ", errors)}");
        }

        public string Id { get; init; }
        public string Name { get; init; }
        public string ModuleType => ModuleLoader.ModuleType.Native;
        public string[] Tags { get; init; }
        public Dictionary<string, string> AdditionalProperties { get; init; }
        public NativeManifestDetails Details { get; init; }
    }
}
