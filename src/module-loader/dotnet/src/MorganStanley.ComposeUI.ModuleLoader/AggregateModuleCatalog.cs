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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorganStanley.ComposeUI.ModuleLoader;

internal class AggregateModuleCatalog : IModuleCatalog
{
    private readonly IEnumerable<IModuleCatalog> _moduleCatalogs;
    private readonly ILogger _logger;

    public AggregateModuleCatalog(
        IEnumerable<IModuleCatalog> moduleCatalogs,
        ILogger? logger = null)
    {
        _moduleCatalogs = moduleCatalogs;
        _logger = logger ?? NullLogger.Instance;
        _lazyEnumerateModules = new Lazy<Task>(EnumerateModulesCore);
    }

    public async Task<IModuleManifest> GetManifest(string moduleId)
    {
        await EnumerateModules();

        return _moduleIdToCatalog.TryGetValue(moduleId, out var catalog)
            ? await catalog.GetManifest(moduleId)
            : throw new ModuleNotFoundException(moduleId);
    }

    public async Task<IEnumerable<string>> GetModuleIds()
    {
        await EnumerateModules();

        return _moduleIdToCatalog.Keys;
    }

    private readonly Lazy<Task> _lazyEnumerateModules;
    private readonly ConcurrentDictionary<string, IModuleCatalog> _moduleIdToCatalog = new();

    private Task EnumerateModules() => _lazyEnumerateModules.Value;

    private async Task EnumerateModulesCore()
    {
        var moduleCatalogs = _moduleCatalogs.ToDictionary(x => x, y => y.GetModuleIds());
        // Services/DI registrations appear in the order they were registered when resolved via IEnumerable<{SERVICE}>
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
        foreach (var moduleCatalog in moduleCatalogs)
        {
            var moduleIds = await moduleCatalog.Value;

            foreach (var moduleId in moduleIds)
            {
                if (!_moduleIdToCatalog.TryGetValue(moduleId, out var catalog))
                {
                    _moduleIdToCatalog[moduleId] = moduleCatalog.Key;
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        var moduleManifest = await catalog.GetManifest(moduleId);
                        _logger.LogWarning(
                            $"ModuleId: {moduleId} is already contained by an another {nameof(IModuleCatalog)} with name {moduleManifest.Name}. Please consider using unique ids for modules. The first occurrence of the module will be saved and used by the {nameof(ModuleLoader)}.");
                    }
                }
            }
        }
    }
}
