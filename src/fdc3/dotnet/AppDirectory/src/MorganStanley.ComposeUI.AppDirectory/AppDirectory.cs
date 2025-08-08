// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.NewtonsoftJson.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public class AppDirectory : IAppDirectory
{
    public AppDirectory(
        IOptions<AppDirectoryOptions> options,
        IHttpClientFactory? httpClientFactory = null,
        IMemoryCache? cache = null,
        IFileSystem? fileSystem = null,
        IHostManifestMapper? hostManifestMapper = null,
        ILogger<AppDirectory>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _fileSystem = fileSystem ?? new FileSystem();
        _hostManifestMapper = hostManifestMapper;
        _logger = logger ?? NullLogger<AppDirectory>.Instance;
    }

    public async Task<IEnumerable<Fdc3App>> GetApps()
    {
        return (await GetAppsCore()).Values;
    }

    public async Task<Fdc3App?> GetApp(string appId)
    {
        if (!(await GetAppsCore()).TryGetValue(appId, out var app))
            throw new AppNotFoundException(appId);

        return app;
    }

    private readonly IHttpClientFactory? _httpClientFactory;
    private HttpClient? _httpClient;
    private readonly IFileSystem _fileSystem;
    private readonly IHostManifestMapper? _hostManifestMapper;
    private readonly ILogger<AppDirectory> _logger;
    private readonly AppDirectoryOptions _options;
    private readonly IMemoryCache _cache;

    private static readonly string GetAppsCacheKey = typeof(AppDirectory).FullName + "." + nameof(GetApps);

    private Task<Dictionary<string, Fdc3App>> GetAppsCore()
    {
        return _cache.GetOrCreateAsync(
            GetAppsCacheKey,
            async entry =>
            {
                var result = await LoadApps();
                if (result.ChangeToken != null)
                {
                    entry.ExpirationTokens.Add(result.ChangeToken);
                }
                else
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheExpirationInSeconds);
                }

                // Assuming that appIds are case-insensitive (not specified by the standard)
                return result.Apps.ToDictionary(
                    app => app.AppId,
                    StringComparer.OrdinalIgnoreCase);
            });
    }

    private Task<(IEnumerable<Fdc3App> Apps, IChangeToken? ChangeToken)> LoadApps()
    {
        if (_options.Source != null)
        {
            if (_options.Source.IsFile && _fileSystem.File.Exists(_options.Source.LocalPath))
            {
                return LoadAppsFromFile(_options.Source.LocalPath);
            }

            if (_options.Source.Scheme == Uri.UriSchemeHttp || _options.Source.Scheme == Uri.UriSchemeHttps)
            {
                return LoadAppsFromHttp(_options.Source.ToString());
            }
        }

        if (!string.IsNullOrEmpty(_options.HttpClientName))
        {
            return LoadAppsFromHttp("apps/");
        }

        _logger.LogError("The configured source is empty or not supported");

        return Task.FromResult<(IEnumerable<Fdc3App>, IChangeToken?)>(
            (
                Enumerable.Empty<Fdc3App>(),
                null));
    }

    private Task<(IEnumerable<Fdc3App>, IChangeToken?)> LoadAppsFromFile(string fileName)
    {
        using var stream = _fileSystem.File.OpenRead(fileName);

        return Task.FromResult<(IEnumerable<Fdc3App>, IChangeToken?)>(
            (
                LoadAppsFromStream(stream, _hostManifestMapper),
                new FileSystemChangeToken(fileName, _fileSystem)));
    }

    private async Task<(IEnumerable<Fdc3App>, IChangeToken?)> LoadAppsFromHttp(string relativeUri)
    {
        var httpClient = GetHttpClient();
        var response = await httpClient.GetAsync(relativeUri);
        var stream = await response.Content.ReadAsStreamAsync();

        return (LoadAppsFromStream(stream, _hostManifestMapper), null);
    }

    private HttpClient GetHttpClient()
    {
        if (_httpClient != null)
            return _httpClient;

        if (!string.IsNullOrEmpty(_options.HttpClientName))
        {
            // Let configuration changes propagate. The factory manages the lifetime of underlying resources.
            return _httpClientFactory?.CreateClient(_options.HttpClientName)
                   ?? throw new InvalidOperationException(
                       $"{nameof(AppDirectoryOptions)} is configured with {nameof(AppDirectoryOptions.HttpClientName)}, but a suitable {nameof(IHttpClientFactory)} was not provided");
        }

        if (_httpClientFactory != null)
            return _httpClientFactory.CreateClient();

        return _httpClient = new HttpClient();
    }

    private static IEnumerable<Fdc3App> LoadAppsFromStream(Stream stream, IHostManifestMapper? hostManifestMapper = null)
    {
        var jsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new Fdc3CamelCaseNamingStrategy()
            },
            Converters =
            [
                    new StringEnumConverter(new CamelCaseNamingStrategy()),
                    new RecipientJsonConverter(),
                    new Fdc3AppConverter(),
            ],
        };

        if (hostManifestMapper != null)
        {
            jsonSerializerSettings.Converters.Add(hostManifestMapper.HostManifestJsonConverter);
        }

        var serializer = JsonSerializer.Create(jsonSerializerSettings);
        using var textReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(textReader);
        jsonReader.Read();

        return jsonReader.TokenType == JsonToken.StartArray
            ? serializer.Deserialize<IEnumerable<Fdc3App>>(jsonReader) ?? Enumerable.Empty<Fdc3App>()
            : serializer.Deserialize<GetAppsJsonResponse>(jsonReader)?.Applications ?? Enumerable.Empty<Fdc3App>();
    }

    private sealed class FileSystemChangeToken : IChangeToken
    {
        public FileSystemChangeToken(string path, IFileSystem fileSystem)
        {
            var fileSystemWatcher = fileSystem.FileSystemWatcher.New(
                fileSystem.Path.GetDirectoryName(path)!,
                fileSystem.Path.GetFileName(path));

            fileSystemWatcher.Changed += (sender, args) =>
            {
                if (!args.ChangeType.HasFlag(WatcherChangeTypes.Changed))
                    return;

                fileSystemWatcher.Dispose();
                HasChanged = true;

                foreach (var item in _callbacks)
                {
                    item.Value();
                }

                _callbacks.Clear(); // Change tokens are only fired once
            };

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public bool HasChanged { get; private set; }
        public bool ActiveChangeCallbacks => true;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            if (HasChanged)
            {
                callback(state);

                return Disposable.Empty;
            }

            var action = () => callback(state);

            _callbacks.TryAdd(action, action);

            return Disposable.Create(() => _callbacks.TryRemove(action, out _));
        }

        private readonly ConcurrentDictionary<object, Action> _callbacks = new();
    }

    /// <summary>
    /// Wrapper type for the /v2/apps response
    /// </summary>
    private sealed class GetAppsJsonResponse
    {
        [JsonProperty("applications")]
        public IEnumerable<Fdc3App>? Applications { get; set; }
    }
}