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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;

/// <summary>
/// Provides methods for creating context listeners and joining channels and doing other channel operations.
/// </summary>
internal interface IChannelFactory
{
    /// <summary>
    /// Creates a context listener for the specified context type.
    /// </summary>
    /// <typeparam name="T">The type of context to listen for.</typeparam>
    /// <param name="contextHandler">The handler to invoke when context is received.</param>
    /// <param name="currentChannel">The channel to listen on. If null, the default channel is used.</param>
    /// <param name="contextType">The context type to filter for. If null, all context types are received.</param>
    /// <returns>A <see cref="ValueTask{ContextListener}"/> representing the asynchronous operation.</returns>
    public ValueTask<ContextListener<T>> CreateContextListener<T>(
        ContextHandler<T> contextHandler,
        IChannel? currentChannel = null,
        string? contextType = null)
        where T : IContext;

    /// <summary>
    /// Joins the user channel with the specified channel ID.
    /// </summary>
    /// <param name="channelId">The ID of the user channel to join.</param>
    /// <returns>A <see cref="ValueTask{IChannel}"/> representing the asynchronous operation.</returns>
    public ValueTask<IChannel> JoinUserChannelAsync(string channelId);

    /// <summary>
    /// Retrieves all available user channels.
    /// </summary>
    /// <returns>A <see cref="ValueTask{IChannel[]}"/> representing the asynchronous operation that returns an array of user channels.</returns>
    public ValueTask<IChannel[]> GetUserChannels();
}
