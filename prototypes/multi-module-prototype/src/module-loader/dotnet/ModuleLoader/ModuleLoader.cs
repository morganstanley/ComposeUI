// ********************************************************************************************************
//
// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
// 
// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;

internal class ModuleLoader : IModuleLoader
{
    private Subject<LifecycleEvent> _lifecycleEvents = new();
    public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents;
    private ConcurrentDictionary<Guid, IModuleHost> _processes = new();
    private readonly IModuleHostFactory _moduleHostFactory;
    private readonly IModuleCatalogue _moduleCatalogue;

    public ModuleLoader(IModuleCatalogue moduleCatalogue, IModuleHostFactory moduleHostFactory)
    {
        _moduleCatalogue = moduleCatalogue;
        _moduleHostFactory = moduleHostFactory;
    }

    public void RequestStartProcess(LaunchRequest request)
    {
        Task.Run(() => StartProcess(request));
    }

    private async void StartProcess(LaunchRequest request)
    {
        IModuleHost host = _processes.GetOrAdd(request.instanceId, _ => CreateModuleHost(request));
        await host.Launch();
    }

    private IModuleHost CreateModuleHost(LaunchRequest request)
    {
        var manifest = _moduleCatalogue.GetManifest(request.name);
        var h = _moduleHostFactory.CreateModuleHost(manifest, request.instanceId);
        h.LifecycleEvents.Subscribe(ForwardLifecycleEvents);
        return h;
    }

    public async void RequestStopProcess(StopRequest request)
    {
        if (!_processes.TryGetValue(request.instanceId, out var module))
        {
            return;
        }
        await module.Teardown();
    }

    private void ForwardLifecycleEvents(LifecycleEvent lifecycleEvent)
    {
        _lifecycleEvents.OnNext(lifecycleEvent);
    }
}
