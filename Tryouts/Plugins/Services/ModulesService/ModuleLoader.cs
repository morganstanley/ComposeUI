/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Reactive.Subjects;

namespace MorganStanley.ComposeUI.Tryouts.Plugins.Services.ModulesService;

public class ModuleLoader : IModuleLoader
{

    private Subject<LifecycleEvent> _lifecycleEvents = new Subject<LifecycleEvent>();
    public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents;
    private Dictionary<Guid, IModule> _processes = new Dictionary<Guid, IModule>();
    private readonly IModuleHostFactory _moduleHostFactory;
    private readonly ModuleCatalogue _moduleCatalogue;

    public ModuleLoader(ModuleCatalogue moduleCatalogue, IModuleHostFactory moduleHostFactory)
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
        var manifest = _moduleCatalogue.GetManifest(request.name);
        IModule host;
        if (!_processes.TryGetValue(request.instanceId, out host))
        {
            host = _moduleHostFactory.CreateModuleHost(manifest);
            _processes.Add(request.instanceId, host);
            await host.Initialize();
            host.LifecycleEvents.Subscribe(ForwardLifecycleEvents);
        }

        await host.Launch();
    }

    public async void RequestStopProcess(StopRequest request)
    {
        IModule? module;
        if (!_processes.TryGetValue(request.instanceId, out module))
        {
            throw new Exception("Unknown process name");
        }
        await module.Teardown();
    }

    private void ForwardLifecycleEvents(LifecycleEvent lifecycleEvent)
    {
        _lifecycleEvents.OnNext(lifecycleEvent);
    }
}
