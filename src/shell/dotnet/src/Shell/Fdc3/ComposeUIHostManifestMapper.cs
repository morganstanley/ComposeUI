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

using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.NewtonsoftJson.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.ModuleLoader;
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal sealed partial class ComposeUIHostManifestMapper : IHostManifestMapper
{
    private readonly string _composeUIHostManifest = "ComposeUI";
    private ILogger<ComposeUIHostManifestMapper> _logger;
    private JsonSerializerSettings _jsonSerializerSettings = new Fdc3JsonSerializerSettings();
    private readonly Dictionary<AppType, IManifestDetailsMapper> _mappers;

    public ComposeUIHostManifestMapper(ILogger<ComposeUIHostManifestMapper>? logger = null)
    {
        _logger = logger ?? NullLogger<ComposeUIHostManifestMapper>.Instance;
        HostManifestJsonConverter = new ComposeUIHostManifestConverter();
        _mappers = new Dictionary<AppType, IManifestDetailsMapper>
        {
            { AppType.Web, new WebManifestDetailsMapper() },
            { AppType.Native, new NativeManifestDetailsMapper() }
        };
    }

    public JsonConverter HostManifestJsonConverter { get; }

    public ModuleDetails MapModuleDetails(Fdc3App fdc3App)
    {
        if (!_mappers.TryGetValue(fdc3App.Type, out var mapper))
        {
            throw new NotSupportedException($"The {fdc3App.Type} is currently not supported for hostmanifest conversion!");
        }

        var iconSrc = fdc3App.Icons?.FirstOrDefault()?.Src;
        ComposeUIHostManifest? composeUIHostManifest = null;

        if (fdc3App.HostManifests != null &&
            fdc3App.HostManifests.TryGetValue(_composeUIHostManifest, out var hostManifest))
        {
            composeUIHostManifest = hostManifest as ComposeUIHostManifest;
        }

        try
        {
            return mapper.Map(fdc3App, composeUIHostManifest, iconSrc);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception was thrown while mapping {fdc3App.Type} manifest details.");
            throw;
        }
    }
}

