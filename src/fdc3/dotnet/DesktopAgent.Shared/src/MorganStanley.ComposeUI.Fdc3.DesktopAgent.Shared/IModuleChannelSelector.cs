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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

/// <summary>
/// Responsible for registering endpoint to handle channel selection requests from the API.
/// </summary>
public interface IModuleChannelSelector : IAsyncDisposable
{
    /// <summary>
    /// Registers a handler to process channel selection requests initiated from desktop agent clients. If the client send a leaveCurrentChannel request then the onChannelJoined callback will be invoked with null or empty string.
    /// </summary>
    /// <param name="fdc3InstanceId"></param>
    /// <param name="onChannelJoined"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask RegisterChannelSelectorHandlerInitiatedFromClientsAsync(string fdc3InstanceId, Action<string?> onChannelJoined, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the client library to handle the channel join request initiated from the UI.
    /// </summary>
    /// <param name="fdc3InstanceId"></param>
    /// <param name="channelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<string?> InvokeJoinUserChannelFromUIAsync(string fdc3InstanceId, string channelId, CancellationToken cancellationToken = default);
}