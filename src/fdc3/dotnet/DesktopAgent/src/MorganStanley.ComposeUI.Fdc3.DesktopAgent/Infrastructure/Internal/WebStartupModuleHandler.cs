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
using MorganStanley.ComposeUI.ModuleLoader;
using ResourceReader = MorganStanley.ComposeUI.Utilities.ResourceReader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// Handles startup logic for web modules by injecting configuration scripts.
/// </summary>
internal sealed class WebStartupModuleHandler : StartupModuleHandler
{
    /// <inheritdoc/>
    public override Task HandleAsync(StartupContext startupContext, string appId, string fdc3InstanceId, string? channelId, string? openedAppContextId)
    {
        var webProperties = startupContext.GetOrAddProperty<WebStartupProperties>();

        var stringBuilder = new StringBuilder();
        stringBuilder.Append($$"""
                    window.composeui.fdc3 = {
                        ...window.composeui.fdc3, 
                        config: {
                            appId: "{{appId}}",
                            instanceId: "{{fdc3InstanceId}}"
                        }
                  """);

        if (channelId != null)
        {
            stringBuilder.Append($$"""
                ,
                channelId: "{{channelId}}"
                """);
        }

        if (openedAppContextId != null)
        {
            stringBuilder.Append($$"""
                ,
                openAppIdentifier: {
                    openedAppContextId: "{{openedAppContextId}}"
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
