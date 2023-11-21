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
using MorganStanley.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public sealed class Fdc3ModuleCatalog : IModuleCatalog
{
    private readonly IAppDirectory _appDirectory;

    public Fdc3ModuleCatalog(IAppDirectory fdc3AppDirectory)
    {
        _appDirectory = fdc3AppDirectory;
    }

    public async Task<IModuleManifest> GetManifest(string moduleId)
    {
        var app = await _appDirectory.GetApp(moduleId);

        switch (app.Type)
        {
            case AppType.Web:
                return new Fdc3WebModuleManifest(app);

            default:
                throw new NotSupportedException($"Unsupported module type: {Enum.GetName(app.Type)}");
        }
    }

    public async Task<IEnumerable<string>> GetModuleIds()
    {
        var apps = await _appDirectory.GetApps();
        return apps.Select(x => x.AppId);
    }

    private class Fdc3WebModuleManifest : IModuleManifest<WebManifestDetails>
    {
        public Fdc3WebModuleManifest(Fdc3App app)
        {
            if (app.Type != AppType.Web)
            {
                throw new ArgumentException("The provided app is not a web app.", nameof(app));
            }

            Id = app.AppId;
            Name = app.Name;

            var iconSrc = app.Icons?.FirstOrDefault()?.Src;
            var url = new Uri(((WebAppDetails) app.Details).Url, UriKind.Absolute);

            Details = new WebManifestDetails
            {
                Url = url,
                IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null
            };
        }

        public WebManifestDetails Details { get; init; }

        public string Id { get; init; }

        public string Name { get; init; }

        public string ModuleType => ModuleLoader.ModuleType.Web;
    }
}
