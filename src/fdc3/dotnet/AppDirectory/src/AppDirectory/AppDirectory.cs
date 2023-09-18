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
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.NewtonsoftJson.Serialization;
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public class AppDirectory : IAppDirectory
{
    public AppDirectory(
        IOptions<AppDirectoryOptions> options,
        IMemoryCache? cache = null,
        IFileSystem? fileSystem = null,
        ILogger<AppDirectory>? logger = null)
    {
        _options = options.Value;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _fileSystem = fileSystem ?? new FileSystem();
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

    private readonly IFileSystem _fileSystem;
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
                entry.ExpirationTokens.Add(result.ChangeToken);

                // Assuming that appIds are case-insensitive (not specified by the standard)
                return result.Apps.ToDictionary(
                    app => app.AppId,
                    StringComparer.OrdinalIgnoreCase); 
            });
    }

    private Task<(IEnumerable<Fdc3App> Apps, IChangeToken ChangeToken)> LoadApps()
    {
        if (_fileSystem.File.Exists(_options.Source?.LocalPath))
        {
            return LoadAppsFromFile(_options.Source.LocalPath);
        }
        // TODO: add provider for online static files and FDC3 AppD API

        _logger.LogWarning("The configured source is empty or not supported");

        return Task.FromResult<(IEnumerable<Fdc3App>, IChangeToken)>(
            (
                Enumerable.Empty<Fdc3App>(),
                NullChangeToken.Singleton));
    }

    private Task<(IEnumerable<Fdc3App>, IChangeToken)> LoadAppsFromFile(string fileName)
    {
        using var stream = _fileSystem.File.OpenRead(fileName);

        return Task.FromResult<(IEnumerable<Fdc3App>, IChangeToken)>(
            (
                LoadAppsFromStream(stream),
                new FileSystemChangeToken(fileName, _fileSystem)));
    }

    private static IEnumerable<Fdc3App> LoadAppsFromStream(Stream stream)
    {
        var serializer = JsonSerializer.Create(new Fdc3JsonSerializerSettings());
        using var textReader = new StreamReader(stream, leaveOpen: true);
        using var jsonReader = new JsonTextReader(textReader);

        return serializer.Deserialize<IEnumerable<Fdc3App>>(jsonReader) ?? Enumerable.Empty<Fdc3App>();
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

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            if (HasChanged)
            {
                callback(state);

                return Disposable.Empty;
            }

            var action = () => callback(state);

            _callbacks.TryAdd(action, action);

            return Disposable.Create(() => _callbacks.Remove(action, out _));
        }

        public bool HasChanged { get; private set; }
        public bool ActiveChangeCallbacks => true;
        private readonly ConcurrentDictionary<object, Action> _callbacks = new();
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return Disposable.Empty;
        }

        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => true;

        public static readonly NullChangeToken Singleton = new();
    }
}