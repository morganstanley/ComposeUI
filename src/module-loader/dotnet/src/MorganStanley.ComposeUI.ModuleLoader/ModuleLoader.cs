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
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorganStanley.ComposeUI.ModuleLoader;

internal sealed class ModuleLoader : IModuleLoader, IAsyncDisposable
{
    private readonly ILogger<ModuleLoader> _logger;
    private readonly Subject<LifetimeEvent> _lifetimeEvents = new();
    private readonly ConcurrentDictionary<Guid, IModuleInstance> _modules = new();
    private readonly Dictionary<string, IModuleRunner> _moduleRunners;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IReadOnlyList<IStartupAction> _startupActions;
    private readonly IReadOnlyList<IShutdownAction> _shutdownActions;

    public ModuleLoader(
        IEnumerable<IModuleCatalog> moduleCatalogs,
        IEnumerable<IModuleRunner> moduleRunners,
        IEnumerable<IStartupAction> startupActions,
        IEnumerable<IShutdownAction> shutdownActions,
        ILogger<ModuleLoader>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalogs);
        ArgumentNullException.ThrowIfNull(moduleRunners);
        ArgumentNullException.ThrowIfNull(startupActions);
        
        _logger = logger ?? NullLogger<ModuleLoader>.Instance;
        _moduleCatalog = new AggregateModuleCatalog(moduleCatalogs, _logger);
        _moduleRunners = moduleRunners.GroupBy(runner => runner.ModuleType).ToDictionary(g => g.Key, g => g.First());
        _startupActions = new List<IStartupAction>(startupActions);
        _shutdownActions = new List<IShutdownAction>(shutdownActions);
    }

    public IObservable<LifetimeEvent> LifetimeEvents => _lifetimeEvents;

    public async Task<IModuleInstance> StartModule(StartRequest request)
    {
        var manifest = await _moduleCatalog.GetManifest(request.ModuleId);

        if (!_moduleRunners.TryGetValue(manifest.ModuleType, out var moduleRunner))
        {
            throw new Exception($"No module runner available for {manifest.ModuleType} module type");
        }

        Guid instanceId = Guid.NewGuid();
        var moduleInstance = new ModuleInstance(instanceId, manifest, request);
        _modules.TryAdd(instanceId, moduleInstance);

        _lifetimeEvents.OnNext(new LifetimeEvent.Starting(moduleInstance));
        var startupContext = new StartupContext(request, moduleInstance);
        startupContext.AddProperty(new UnexpectedStopCallback(moduleInstance, HandleUnexpectedStop));

        var pipeline = _startupActions
            .Reverse()
            .Aggregate(
                () => Task.CompletedTask,
                (next, action) => () => action.InvokeAsync(startupContext, next));

        await moduleRunner.Start(startupContext, pipeline);
        moduleInstance.AddProperties(startupContext.GetProperties());
        _lifetimeEvents.OnNext(new LifetimeEvent.Started(moduleInstance));

        return moduleInstance;
    }

    private void HandleUnexpectedStop(IModuleInstance instance)
    {
        _lifetimeEvents.OnNext(new LifetimeEvent.Stopped(instance, false));
    }

    public async Task StopModule(StopRequest request)
    {
        //TODO: decide if we want to remove it from the dictionary
        if (!_modules.TryGetValue(request.InstanceId, out var module))
        {
            return;
        }

        await StopModuleInternal(module, request.Properties);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var module in _modules.Values)
        {
            await StopModuleInternal(module);
        }
        _modules.Clear();
        _lifetimeEvents.Dispose();
    }

    private async Task StopModuleInternal(IModuleInstance moduleInstance, List<object>? properties = null)
    {
        _lifetimeEvents.OnNext(new LifetimeEvent.Stopping(moduleInstance));

        var shutdownContext = new ShutdownContext(moduleInstance);

        if (properties != null)
        {
            foreach (var property in properties)
            {
                shutdownContext.AddProperty(property);
            }
        }

        var pipeline = _shutdownActions
            .Reverse()
            .Aggregate(
             () => Task.CompletedTask,
                (next, action) => () => action.InvokeAsync(shutdownContext, next));

        await Task.Run(async () => await pipeline());

        if (!_modules.TryRemove(moduleInstance.InstanceId, out _))
        {
            _logger.LogError($"Could not remove module, instanceId: {moduleInstance.InstanceId}.");
        }

        await _moduleRunners[moduleInstance.Manifest.ModuleType].Stop(moduleInstance);

        _lifetimeEvents.OnNext(new LifetimeEvent.Stopped(moduleInstance));
    }
}
