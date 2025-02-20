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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.Abstractions;

namespace MorganStanley.ComposeUI.Shell.Modules;

internal sealed class ModuleCatalog : IModuleCatalog, IInitializeAsync
{
    public ModuleCatalog(IOptions<ModuleCatalogOptions> options, IFileSystem? fileSystem = null)
    {
        _options = options.Value;
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public async Task InitializeAsync()
    {
        if (_options.CatalogUrl == null)
            return;

        if (_options.CatalogUrl.Scheme != "file")
            throw new InvalidOperationException(
                "Cannot load the module catalog from the provided URL. Only local files are supported.");

        await LoadFromFile(_options.CatalogUrl.LocalPath);
    }

    public Task<IModuleManifest> GetManifest(string moduleId)
    {
        return Task.FromResult<IModuleManifest>(_modules[moduleId]);
    }

    public Task<IEnumerable<string>> GetModuleIds()
    {
        return Task.FromResult<IEnumerable<string>>(_modules.Keys);
    }

    private readonly IFileSystem _fileSystem;
    private readonly ModuleCatalogOptions _options;
    private Dictionary<string, ModuleManifest> _modules = new();

    private async Task LoadFromFile(string path)
    {
        await using var stream = _fileSystem.File.OpenRead(path);

        var moduleManifests = await JsonSerializer.DeserializeAsync<ModuleManifest[]>(
                              stream,
                              JsonSerializerOptions);

        if (moduleManifests == null)
        {
            _modules = [];
            return;
        }

        foreach (var moduleManifest in moduleManifests.OfType<NativeModuleManifest>())
        {
            if (!moduleManifest.Details.Path.IsAbsoluteUri)
            {
                moduleManifest.Details.Path = new Uri(Path.GetFullPath(moduleManifest.Details.Path.ToString(), Path.GetDirectoryName(path)!));
            }
        }

        _modules = moduleManifests.ToDictionary(m => m.Id);
    }

    internal void Add(ModuleManifest manifest)
    {
        _modules.Add(manifest.Id, manifest);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { Converters = { new ModuleManifestConverter() } };

    private class NativeModuleManifest : ModuleManifest, IModuleManifest<NativeManifestDetails>
    {
        public NativeManifestDetails Details { get; set; }
    }

    private class ModuleManifestConverter : JsonConverter<ModuleManifest>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ModuleManifest);
        }

        public override ModuleManifest? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var typeReader = reader;
            var header = JsonSerializer.Deserialize<ManifestTypeHelper>(ref typeReader, options);

            return header.ModuleType switch
            {
                ModuleType.Web => JsonSerializer.Deserialize<WebModuleManifest>(ref reader, options),
                ModuleType.Native => JsonSerializer.Deserialize<NativeModuleManifest>(ref reader, options),
                _ => throw new InvalidOperationException("Unsupported module type: " + header.ModuleType),
            };
        }

        public override void Write(Utf8JsonWriter writer, ModuleManifest value, JsonSerializerOptions options)
        {
            if (value is WebModuleManifest webModuleManifest)
            {
                JsonSerializer.Serialize(writer, webModuleManifest, options);

                return;
            }

            JsonSerializer.Serialize(writer, value as IModuleManifest, options);
        }

        private struct ManifestTypeHelper
        {
            public string ModuleType { get; set; }
        }
    }
}