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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure;
using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal interface IFdc3DesktopAgentBridge
{
    /// <summary>
    /// Triggers the necessary events like ModuleLoader's Subscribe when Startup.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Disposes the disposabel resources.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Moves the started modules from the queue to the running modules collection after the RaiseIntent call is finished.
    /// </summary>
    /// <returns></returns>
    public ValueTask AddModuleAsync();

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
    public ValueTask<FindIntentResponse> FindIntent(FindIntentRequest request);

    /// <summary>
    /// Handles the FindIntentsByContext call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<FindIntentsByContextResponse> FindIntentsByContext(FindIntentsByContextRequest request);

    /// <summary>
    /// Handles the GetIntentResult call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<GetIntentResultResponse> GetIntentResult(GetIntentResultRequest request);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<KeyValuePair<RaiseIntentResponse, RaiseIntentResolutionMessage?>> RaiseIntent(RaiseIntentRequest request);

    /// <summary>
    /// Handles the AddIntentListener call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<KeyValuePair<AddIntentListenerResponse, IEnumerable<RaiseIntentResolutionMessage>?>> AddIntentListener(AddIntentListenerRequest request);

    /// <summary>
    /// Handles the StoreIntentResult call in the bridge.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public ValueTask<StoreIntentResultResponse> StoreIntentResult(StoreIntentResultRequest request);
}