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

using System;
using System.Linq;
using System.Security.Policy;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.NewtonsoftJson.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.ModuleLoader;
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal sealed class ComposeUIHostManifestMapper : IHostManifestMapper
{
    private readonly string _composeUIHostManifest = "ComposeUI";
    private ILogger<ComposeUIHostManifestMapper> _logger;
    private JsonSerializerSettings _jsonSerializerSettings = new Fdc3JsonSerializerSettings();

    public ComposeUIHostManifestMapper(ILogger<ComposeUIHostManifestMapper>? logger = null)
    {
        _logger = logger ?? NullLogger<ComposeUIHostManifestMapper>.Instance;
        HostManifestJsonConverter = new ComposeUIHostManifestConverter();
    }

    public JsonConverter HostManifestJsonConverter { get; }

    public ModuleDetails MapModuleDetails(Fdc3App fdc3App)
    {
        return fdc3App.Type switch
        {
            AppType.Web => MapWebManifestDetails(fdc3App),
            AppType.Native => MapNativeManifestDetails(fdc3App),
            _ => throw new NotSupportedException($"The {fdc3App.Type} is currently not supported for hostmanifest conversion!"),
        };
    }

    private ModuleDetails MapNativeManifestDetails(Fdc3App fdc3App)
    {
        var iconSrc = fdc3App.Icons?.FirstOrDefault()?.Src;
        var path = new Uri(((NativeAppDetails) fdc3App.Details).Path, UriKind.RelativeOrAbsolute);
        var arguments = ((NativeAppDetails) fdc3App.Details).Arguments;

        var nativeManifestDetails = new NativeManifestDetails
        {
            Path = path,
            Icon = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
            Arguments = arguments?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(), //TODO discuss why the Fdc3DotNet only returns a string instead of an array of string
        };

        if (fdc3App.HostManifests == null
            || !fdc3App.HostManifests.TryGetValue(_composeUIHostManifest, out var hostManifest))
        {
            return nativeManifestDetails;
        }

        try
        {
            var composeUIHostmanifest = hostManifest as ComposeUIHostManifest;

            return new NativeManifestDetails
            {
                Path = path,
                Icon = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                Arguments = arguments?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
                InitialModulePosition = composeUIHostmanifest?.InitialModulePosition,
                Height = composeUIHostmanifest?.Height,
                Width = composeUIHostmanifest?.Width,
                Coordinates = composeUIHostmanifest?.Coordinates,
                EnvironmentVariables = composeUIHostmanifest?.EnvironmentVariables ?? new Dictionary<string, string>()
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception was thrown while executing {nameof(MapNativeManifestDetails)}.");
        }

        return nativeManifestDetails;
    }

    private ModuleDetails MapWebManifestDetails(Fdc3App fdc3App)
    {
        var iconSrc = fdc3App.Icons?.FirstOrDefault()?.Src;
        var url = new Uri(((WebAppDetails) fdc3App.Details).Url, UriKind.Absolute);

        if (fdc3App.HostManifests == null
            || !fdc3App.HostManifests.TryGetValue(_composeUIHostManifest, out var hostManifest))
        {
            return new WebManifestDetails
            {
                Url = url,
                IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
            };
        }

        try
        {
            var composeUIHostmanifest = hostManifest as ComposeUIHostManifest;

            return new WebManifestDetails
            {
                Url = url,
                IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
                InitialModulePosition = composeUIHostmanifest?.InitialModulePosition,
                Height = composeUIHostmanifest?.Height,
                Width = composeUIHostmanifest?.Width,
                Coordinates = composeUIHostmanifest?.Coordinates,
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Exception was thrown while executing {nameof(MapWebManifestDetails)}.");
        }

        return new WebManifestDetails
        {
            Url = url,
            IconUrl = iconSrc != null ? new Uri(iconSrc, UriKind.Absolute) : null,
        };
    }
}
