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
    private readonly ConcurrentDictionary<Guid, IModuleRunner> _modules = new();
    private readonly IEnumerable<IModuleRunner> _moduleRunners;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly IReadOnlyList<IStartupAction> _startupActions;

    public ModuleLoader(
        IModuleCatalog moduleCatalog,
        IEnumerable<IModuleRunner> moduleRunners,
        IEnumerable<IStartupAction> startupActions)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog);
        ArgumentNullException.ThrowIfNull(moduleRunners);

        _moduleCatalog = moduleCatalog;
        _moduleRunners = moduleRunners;
        _startupActions = new List<IStartupAction>(startupActions);
    }

    public IObservable<LifetimeEvent> LifetimeEvents => _lifetimeEvents;

    public Task<IModuleInstance> StartModule(StartRequest request)
    {
        return StartProcess(request);
    }

    public async Task StopModule(StopRequest request)
    {
        if (!_modules.TryGetValue(request.InstanceId, out var module))
        {
            return;
        }

        await module.Stop();
    }

    public async ValueTask DisposeAsync()
    {
        _lifetimeEvents.Dispose();
        foreach (var item in _modules.Values)
        {
            await item.Stop();
        }
    }

    private async Task<IModuleInstance> StartProcess(StartRequest request)
    {
        var manifest = _moduleCatalog.GetManifest(request.ModuleId);
        if (manifest == null)
        {
            throw new Exception($"Unknown Module id: {request.ModuleId}");
        }

        var moduleRunner = _moduleRunners.FirstOrDefault(runner => runner.ModuleType == manifest.ModuleType);
        if (moduleRunner == null)
        {
            throw new Exception($"No module runner available for {manifest.ModuleType} module type");
        }

        Guid instanceId = Guid.NewGuid();
        var moduleInstance = new ModuleInstance(instanceId, manifest, request);

        _lifetimeEvents.OnNext(new LifetimeEvent.Starting(moduleInstance));
        var startupContext = new StartupContext(request);

        int index = -1;
        Func<Task> nextAction = null!;
        nextAction = () =>
        {
            index++;
            return index < _startupActions.Count
                ? _startupActions[index].InvokeAsync(startupContext, nextAction)
                : Task.CompletedTask;
        };

        await moduleRunner.Start(moduleInstance, startupContext, nextAction);
        moduleInstance.SetProperties(startupContext.GetProperties());
        _lifetimeEvents.OnNext(new LifetimeEvent.Started(moduleInstance));

        return moduleInstance;
    }
}
