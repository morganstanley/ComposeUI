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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Utilities;
using MorganStanley.Fdc3.AppDirectory;
using ResourceReader = MorganStanley.ComposeUI.Utilities.ResourceReader;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

internal sealed class Fdc3StartupAction : IStartupAction
{
    private readonly IAppDirectory _appDirectory;

    public Fdc3StartupAction(IAppDirectory appDirectory)
    {
        _appDirectory = appDirectory;
    }

    public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
    {
        if (startupContext.ModuleInstance.Manifest.ModuleType == ModuleType.Web)
        {
            //TODO: should add some identifier to the query => "fdc3:" + startupContext.StartRequest.ModuleId
            var appId = (await _appDirectory.GetApp(startupContext.StartRequest.ModuleId)).AppId;

            var fdc3InstanceId = startupContext.StartRequest.Parameters.FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId).Value ?? Guid.NewGuid().ToString();

            var fdc3StartupProperties = new Fdc3StartupProperties() { InstanceId = fdc3InstanceId};
            fdc3InstanceId = startupContext.GetOrAddProperty<Fdc3StartupProperties>(_ => fdc3StartupProperties).InstanceId;

            var webProperties = startupContext.GetOrAddProperty<WebStartupProperties>();

            webProperties
                .ScriptProviders.Add(_ =>
                    new ValueTask<string>(
                        $$"""
                        window.composeui.fdc3 = {
                            ...window.composeui.fdc3, 
                            config: {
                                appId: "{{appId}}",
                                instanceId: "{{fdc3InstanceId}}"
                            }
                        };
                        """));

            webProperties
                .ScriptProviders.Add(_ => new ValueTask<string>(ResourceReader.ReadResource(ResourceNames.Fdc3Bundle)));
        }

        await (next.Invoke());
    }
}