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

using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Extensions;

/// <summary>
/// Helper class to retrieve FDC3 related properties from the startup context, app directory and user channel set, and to store them in the startup context for later use by specific module handlers.
/// </summary>
public static class StartupContextExtensions
{
    /// <summary>
    /// Extracts FDC3 related properties such as AppId, InstanceId, ChannelId and OpenedAppContextId from the startup context, app directory and user channel set. 
    /// It also stores the retrieved properties in the startup context for later use by specific module handlers.
    /// </summary>
    /// <param name="startupContext"></param>
    /// <param name="appDirectory"></param>
    /// <param name="userChannelSetReader"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<Fdc3StartupProperties> GetFdc3Properties(
        this StartupContext startupContext, 
        IAppDirectory appDirectory, 
        IUserChannelSetReader userChannelSetReader,
        ILogger? logger = null)
    {
        try
        {
            var appId = (await appDirectory.GetApp(startupContext.StartRequest.ModuleId).ConfigureAwait(false)).AppId;
            var userChannelSet = await userChannelSetReader.GetUserChannelSet().ConfigureAwait(false);

            var fdc3InstanceId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId).Value
                                 ?? Guid.NewGuid().ToString();

            var channelId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3ChannelId).Value
                    ?? userChannelSet.FirstOrDefault().Key;

            var openedAppContextId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(x => x.Key == Fdc3StartupParameters.OpenedAppContextId).Value;

            var fdc3StartupProperties = new Fdc3StartupProperties { InstanceId = fdc3InstanceId, ChannelId = channelId, OpenedAppContextId = openedAppContextId };
            fdc3InstanceId = startupContext.GetOrAddProperty<Fdc3StartupProperties>(_ => fdc3StartupProperties).InstanceId;

            return new Fdc3StartupProperties()
            {
                AppId = appId,
                ChannelId = channelId,
                InstanceId = fdc3InstanceId,
                OpenedAppContextId = openedAppContextId
            };
        }
        catch (AppNotFoundException exception)
        {
            throw exception;
        }
    }
}
