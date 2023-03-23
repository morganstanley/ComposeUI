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

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService.Hosts;

internal class WebpageModuleHost : ModuleHostBase
{
    private readonly string _url;
    private readonly IModuleRunner? _runner;

    public WebpageModuleHost(string name, Guid instanceId, string url, IModuleRunner? runner) : base(name, instanceId)
    {
        _url = url;
        _runner = runner;
    }



    public override ProcessInfo ProcessInfo => new ProcessInfo(
        name: Name,
        instanceId: InstanceId,
        uiType: UIType.Web,
        uiHint: _url,
        pid: 0 //doesn't exist yet
        );
    
    public async override Task Launch()
    {
        int pid = 0;
        if (_runner != null)
        {
            pid = await _runner.Launch();
        }

        _lifecycleEvents.OnNext(LifecycleEvent.Started(new ProcessInfo(Name, InstanceId, UIType.Web, _url, pid)));
    }

    public async override Task Teardown()
    {
        if (_runner != null)
        {
            await _runner.Stop();
        }
        _lifecycleEvents.OnNext(LifecycleEvent.Stopped(new ProcessInfo(Name, InstanceId, UIType.Web, _url, 0))); //stopped --> doesn't exists
    }
}
