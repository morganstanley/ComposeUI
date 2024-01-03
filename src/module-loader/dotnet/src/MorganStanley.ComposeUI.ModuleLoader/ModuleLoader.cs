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

namespace MorganStanley.ComposeUI.ModuleLoader;

internal sealed class ModuleLoader : IModuleLoader, IAsyncDisposable
{
    private readonly Subject<LifetimeEvent> _lifetimeEvents = new();
    private readonly ConcurrentDictionary<Guid, IModuleInstance> _modules = new();
    private readonly Dictionary<string, IModuleRunner> _moduleRunners;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IReadOnlyList<IStartupAction> _startupActions;

    public ModuleLoader(
        IModuleCatalog moduleCatalog,
        IEnumerable<IModuleRunner> moduleRunners,
        IEnumerable<IStartupAction> startupActions)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog);
        ArgumentNullException.ThrowIfNull(moduleRunners);
        ArgumentNullException.ThrowIfNull(startupActions);

        _moduleCatalog = moduleCatalog;
        _moduleRunners = moduleRunners.GroupBy(runner => runner.ModuleType).ToDictionary(g => g.Key, g => g.First());
        _startupActions = new List<IStartupAction>(startupActions);
    }

    public IObservable<LifetimeEvent> LifetimeEvents => _lifetimeEvents;

    public async Task<IModuleInstance> StartModule(StartRequest request)
    {
        var manifest = await _moduleCatalog.GetManifest(request.ModuleId);
        if (manifest == null)
        {
            throw new Exception($"Unknown Module id: {request.ModuleId}");
        }

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
        if (!_modules.TryGetValue(request.InstanceId, out var module))
        {
            return;
        }

        await StopModuleInternal(module);
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

    private async Task StopModuleInternal(IModuleInstance moduleInstance)
    {
        _lifetimeEvents.OnNext(new LifetimeEvent.Stopping(moduleInstance));

        await _moduleRunners[moduleInstance.Manifest.ModuleType].Stop(moduleInstance);

        _lifetimeEvents.OnNext(new LifetimeEvent.Stopped(moduleInstance));
    }
}
