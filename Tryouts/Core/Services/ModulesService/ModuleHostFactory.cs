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
using MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService.Hosts;
using MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService.Runners;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService;

internal class ModuleHostFactory : IModuleHostFactory
{
    public IModuleHost CreateModuleHost(ModuleManifest manifest, Guid instanceId)
    {
        IModuleRunner runner;
        switch (manifest.StartupType)
        {
            case (StartupType.Executable):
                runner = new ExecutableRunner(manifest.Path, manifest.Arguments);
                break;
            case (StartupType.DotNetCore):
                runner = new DotNetCoreRunner(manifest.Path, manifest.Arguments);
                break;
            case (StartupType.SelfHostedWebApp):
                runner = new ComposeHostedWebApp(manifest.Path, manifest.Port.Value);
                break;
            case (StartupType.None):
                runner = null;
                break;
            default:
                throw new NotSupportedException("Unsupported startup type");

        }

        switch (manifest.UIType)
        {
            case (UIType.Window):
                return new WindowedModuleHost(manifest.Name, instanceId, runner as IWindowedModuleRunner);
            case (UIType.Web):
                return new WebpageModuleHost(manifest.Name, instanceId, manifest.Url, runner);
            case (UIType.None):
                return new BackgroundModuleHost(manifest.Name, instanceId, runner);
            default:
                throw new NotSupportedException("Unsupported module type");
        }
    }
}
