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

using System.Text;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;
using ResourceReader = MorganStanley.ComposeUI.Utilities.ResourceReader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// Handles startup logic for web modules by injecting configuration scripts.
/// </summary>
internal sealed class WebStartupModuleHandler : IStartupModuleHandler
{
    /// <inheritdoc/>
    public Task HandleAsync(StartupContext startupContext, Fdc3StartupProperties fdc3StartupProperties)
    {
        var webProperties = startupContext.GetOrAddProperty<WebStartupProperties>();

        var stringBuilder = new StringBuilder();
        stringBuilder.Append($$"""
                    window.composeui.fdc3 = {
                        ...window.composeui.fdc3, 
                        config: {
                            appId: "{{fdc3StartupProperties.AppId}}",
                            instanceId: "{{fdc3StartupProperties.InstanceId}}"
                        }
                  """);

        if (fdc3StartupProperties.ChannelId != null)
        {
            stringBuilder.Append($$"""
                ,
                channelId: "{{fdc3StartupProperties.ChannelId}}"
                """);
        }

        if (fdc3StartupProperties.OpenedAppContextId != null)
        {
            stringBuilder.Append($$"""
                ,
                openAppIdentifier: {
                    openedAppContextId: "{{fdc3StartupProperties.OpenedAppContextId}}"
                }
                """);
        }

        stringBuilder.Append($$"""
            };
            """);

        stringBuilder.AppendLine();
        stringBuilder.Append(ResourceReader.ReadResource(ResourceNames.Fdc3Bundle));

        webProperties.ScriptProviders.Add(_ => new ValueTask<string>(stringBuilder.ToString()));

        return Task.CompletedTask;
    }
}
