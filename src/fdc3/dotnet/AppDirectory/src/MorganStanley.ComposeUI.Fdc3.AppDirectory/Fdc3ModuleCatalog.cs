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

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public sealed class Fdc3ModuleCatalog : IModuleCatalog
{
    private readonly IAppDirectory _appDirectory;
    private readonly IHostManifestMapper? _hostManifestMapper;
    private readonly ILogger<Fdc3ModuleCatalog> _logger;

    public Fdc3ModuleCatalog(
        IAppDirectory fdc3AppDirectory,
        IHostManifestMapper? hostManifestMapper = null,
        ILogger<Fdc3ModuleCatalog>? logger = null)
    {
        _appDirectory = fdc3AppDirectory;
        _hostManifestMapper = hostManifestMapper;
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
        AppType.Native => new Fdc3NativeModuleManifest(app, _hostManifestMapper),
        _ => throw new NotSupportedException($"Unsupported module type: {app.Type}"),
    };

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

                if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                {
                    throw new NotSupportedException($"The web app {app.AppId} must use an http or https URL, but its URL scheme is {url.Scheme}.");
                }

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
            catch (NotSupportedException exception)
            {
                // One unusable app must not hide all the others.
                _logger.LogWarning("The app {AppId} is left out of the module list: {Reason}", app.AppId, exception.Message);
            }
        }

        return manifests;
    }


    private class Fdc3NativeModuleManifest : IModuleManifest<NativeManifestDetails>
    {
        private readonly IHostManifestMapper? _hostManifestMapper;

        public Fdc3NativeModuleManifest(Fdc3App app, IHostManifestMapper hostManifestMapper)
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
                var path = new Uri(nativeDetails.Path, UriKind.RelativeOrAbsolute);

                // TODO: Get the root directory as quick turnaround, but this should be improved to support more scenarios - like when the app is not located under the source directory.
                var repositoryRoot = Environment.GetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME);

                if (string.IsNullOrEmpty(repositoryRoot))
                {
                    repositoryRoot = Consts.COMPOSEUI_MODULE_REPOSITORY_DEFAULT_ROOT;
                }

                var fullPath = path.IsAbsoluteUri
                    ? path.LocalPath
                    : Path.GetFullPath(nativeDetails.Path);

                if (!fullPath.StartsWith(repositoryRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException($"Native app path must be under the ComposeUI executable directory: {repositoryRoot}, and it was under: {fullPath}.");
                }

                Details = new NativeManifestDetails
                {
                    Path = path,
                    Icon = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                };
            }
        }

        public string Id { get; init; }
        public string Name { get; init; }
        public string ModuleType => ModuleLoader.ModuleType.Native;
        public string[] Tags { get; init; }
        public Dictionary<string, string> AdditionalProperties { get; init; }
        public NativeManifestDetails Details { get; init; }
    }
}
