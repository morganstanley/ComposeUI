/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using Finos.Fdc3;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal interface IFdc3DesktopAgentBridge
{
    /// <summary>
    /// Triggers the necessary events like ModuleLoader's Subscribe when Startup.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disposes the disposable resources.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles the AddUserChannel call in the bridge.
    /// </summary>
    /// <param name="addUserChannelFactory"></param>
    /// <param name="channelId"></param>
    /// <returns></returns>
    public ValueTask<UserChannel?> AddUserChannel(Func<string, UserChannel> addUserChannelFactory, string channelId);

    /// <summary>
    /// Handles the AddPrivateChannel call in the bridge.
    /// </summary>
    /// <param name="addPrivateChannelFactory"></param>
    /// <returns></returns>
    public ValueTask AddPrivateChannel(Func<string, PrivateChannel> addPrivateChannelFactory, string privateChannelId);

    /// <summary>
    /// Handles the AddAppChannel call in the bridge.
    /// </summary>
    /// <param name="addAppChannelFactory"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<CreateAppChannelResponse> AddAppChannel(Func<string, AppChannel> addAppChannelFactory, CreateAppChannelRequest request);

    /// <summary>
    /// Handles the FindChannel call in the bridge.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="channelType"></param>
    /// <returns></returns>
    public bool FindChannel(string channelId, ChannelType channelType);

    /// <summary>
    /// Handles the FindIntent call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<FindIntentResponse> FindIntent(FindIntentRequest? request);

    /// <summary>
    /// Handles the FindIntentsByContext call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<FindIntentsByContextResponse> FindIntentsByContext(FindIntentsByContextRequest? request);

    /// <summary>
    /// Handles the GetIntentResult call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetIntentResultResponse> GetIntentResult(GetIntentResultRequest? request);

    /// <summary>
    /// Handles the RaiseIntent call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<RaiseIntentResult<RaiseIntentResponse>> RaiseIntent(RaiseIntentRequest? request);

    /// <summary>
    /// Handles the AddIntentListener call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<RaiseIntentResult<IntentListenerResponse>> AddIntentListener(IntentListenerRequest? request);

    /// <summary>
    /// Handles the StoreIntentResult call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<StoreIntentResultResponse> StoreIntentResult(StoreIntentResultRequest? request);

    /// <summary>
    /// Handles the GetUserChannels call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetUserChannelsResponse> GetUserChannels(GetUserChannelsRequest? request);

    /// <summary>
    /// Handles the JoinUserChannel call in the bridge.
    /// </summary>
    /// <param name="addUserChannelFactory"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<JoinUserChannelResponse?> JoinUserChannel(Func<string, UserChannel> addUserChannelFactory, JoinUserChannelRequest request);

    /// <summary>
    /// Handles the GetInfo call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetInfoResponse> GetInfo(GetInfoRequest? request);

    /// <summary>
    /// Handles the FindInstances call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<FindInstancesResponse> FindInstances(FindInstancesRequest? request);

    /// <summary>
    /// Handles the GetAppMetadata call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetAppMetadataResponse> GetAppMetadata(GetAppMetadataRequest? request);

    /// <summary>
    /// Handles the AddContextListener call in the bridge. It enables tracking the added contextListeners using the `fdc3.addContextListener`.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<AddContextListenerResponse?> AddContextListener(AddContextListenerRequest? request);

    /// <summary>
    /// Handles the ContextListener action (join/unsubscribe to a channel) call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<RemoveContextListenerResponse?> RemoveContextListener(RemoveContextListenerRequest? request);

    //TODO:Context deserialization
    /// <summary>
    /// Handles the Open call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public ValueTask<OpenResponse?> Open(OpenRequest? request, IContext? context = null);

    /// <summary>
    /// Handles the GetOpenedAppContext call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetOpenedAppContextResponse?> GetOpenedAppContext(GetOpenedAppContextRequest? request);

    //TODO:Context deserialization
    /// <summary>
    /// Handles the RaiseIntentForContext call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<RaiseIntentResult<RaiseIntentResponse>> RaiseIntentForContext(RaiseIntentForContextRequest? request, IContext context);
}