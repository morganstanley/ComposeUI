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

using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// Handles startup logic for native modules by setting environment variables.
/// </summary>
internal sealed class NativeStartupModuleHandler : StartupModuleHandler
{
    /// <inheritdoc/>
    public override Task HandleAsync(StartupContext startupContext, string appId, string fdc3InstanceId, string? channelId, string? openedAppContextId)
    {
        var nativeProperties = startupContext.GetOrAddProperty<NativeStartupProperties>();

        nativeProperties.EnvironmentVariables.Add(nameof(AppIdentifier.AppId), appId);
        nativeProperties.EnvironmentVariables.Add(nameof(AppIdentifier.InstanceId), fdc3InstanceId);

        if (channelId != null)
        {
            nativeProperties.EnvironmentVariables.Add(nameof(Fdc3StartupProperties.ChannelId), channelId);
        }

        if (openedAppContextId != null)
        {
            nativeProperties.EnvironmentVariables.Add(nameof(Fdc3StartupProperties.OpenedAppContextId), openedAppContextId);
        }

        return Task.CompletedTask;
    }
}
