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

using MorganStanley.ComposeUI.Messaging;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure;

internal delegate ValueTask SubscribeHandler(ReadOnlySpan<byte> message);

internal interface IMessagingService
{
    /// <summary>
    /// Client Id, which identifies the messaging service's instance.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Publishes messages with the <seealso cref="IMessagingService"/> instance.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask SendMessageAsync(string endpoint, ReadOnlySpan<byte> message, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to messages on a given endpoint with the <seealso cref="IMessagingService"/> instance.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="handler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IAsyncDisposable> SubscribeAsync(string endpoint, SubscribeHandler handler, CancellationToken cancellationToken);

    /// <summary>
    /// Connects to the <seealso cref="IMessagingService"/> instance.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registers a service handler.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="endpoint"></param>
    /// <param name="handler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask RegisterServiceAsync<TRequest>(string endpoint, Func<TRequest?, ValueTask<byte[]?>> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a service.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken);
}