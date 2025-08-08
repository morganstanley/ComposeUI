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

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public sealed class Fdc3ModuleCatalog : IModuleCatalog
{
    private readonly IAppDirectory _appDirectory;
    private readonly IHostManifestMapper? _hostManifestMapper;

    public Fdc3ModuleCatalog(
        IAppDirectory fdc3AppDirectory,
        IHostManifestMapper? hostManifestMapper = null)
    {
        _appDirectory = fdc3AppDirectory;
        _hostManifestMapper = hostManifestMapper;
    }

    public async Task<IModuleManifest> GetManifest(string moduleId)
    {
        var app = await _appDirectory.GetApp(moduleId);
        return GetManifest(app);
    }

    private IModuleManifest GetManifest(Fdc3App app) => app.Type switch
    {
        AppType.Web => new Fdc3WebModuleManifest(app, _hostManifestMapper),
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

        return apps.Select(GetManifest);
    }
}
