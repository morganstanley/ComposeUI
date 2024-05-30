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
    /// <param name="userChannel"></param>
    /// <returns></returns>
    public ValueTask AddUserChannel(UserChannel userChannel);

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
}