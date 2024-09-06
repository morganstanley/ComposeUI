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

using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class UserChannelSetReader : IUserChannelSetReader, IDisposable
{
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserChannelSetReader> _logger;
    private IReadOnlyDictionary<string, ChannelItem>? _userChannelSet;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DisplayMetadataJsonConverter(), new JsonStringEnumConverter() }
    };

    public UserChannelSetReader(
        IOptions<Fdc3DesktopAgentOptions> options,
        IFileSystem? fileSystem = null, 
        ILogger<UserChannelSetReader>? logger = null)
    {
        _options = options.Value;
        _fileSystem = fileSystem ?? new FileSystem();
        _httpClient = new HttpClient();
        _logger = logger ?? NullLogger<UserChannelSetReader>.Instance;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<IReadOnlyDictionary<string, ChannelItem>> GetUserChannelSet(CancellationToken cancellationToken = default)
    {
        if (_userChannelSet != null)
        {
            return _userChannelSet;
        }

        var uri = _options.UserChannelConfigFile;

        if (uri == null && _options.UserChannelConfig == null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(ResourceNames.DefaultUserChannelSet);
            if (stream != null)
            {
                var userChannels = JsonSerializer.Deserialize<ChannelItem[]>(stream, _jsonSerializerOptions);
                _userChannelSet = userChannels?.ToDictionary(x => x.Id, y => y);
            }
        }
        else if (_options.UserChannelConfig != null)
        {
            _userChannelSet = _options.UserChannelConfig.ToDictionary(x => x.Id, y => y);
        }
        else if (uri != null)
        {
            if (uri.IsFile)
            {
                var path = uri.IsAbsoluteUri ? uri.AbsolutePath : Path.GetFullPath(uri.ToString());

                if (_fileSystem.File.Exists(path))
                {
                    await using var stream = _fileSystem.File.OpenRead(path);
                    _userChannelSet = (JsonSerializer.Deserialize<ChannelItem[]>(stream, _jsonSerializerOptions))?.ToDictionary(x => x.Id, y => y);
                }
            }
            else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                var response = await _httpClient.GetAsync(uri, cancellationToken);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                _userChannelSet = (JsonSerializer.Deserialize<ChannelItem[]>(stream, _jsonSerializerOptions))?.ToDictionary(x => x.Id, y => y);
            }
        }

        return _userChannelSet ?? ImmutableDictionary<string, ChannelItem>.Empty;
    }
}
