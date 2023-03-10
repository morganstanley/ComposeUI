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

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
///     Message Router client interface.
/// </summary>
public interface IMessageRouter : IAsyncDisposable
{
    /// <summary>
    /// Gets the client ID of the current connection.
    /// </summary>
    /// <remarks>
    /// The returned value will be <value>null</value> if the client is not connected.
    /// </remarks>
    string? ClientId { get; }

    /// <summary>
    ///     Asynchronously connects to the Message Router server endpoint.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Clients don't need to call this method before calling other methods on this type.
    ///     The client should automatically establish a connection when needed.
    /// </remarks>
    ValueTask ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an observable that represents a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<IDisposable> SubscribeAsync(
        string topic,
        ISubscriber<TopicMessage> subscriber,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes a message to a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask PublishAsync(string topic, MessageBuffer? payload = null, PublishOptions options = default, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invokes a named service.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<MessageBuffer?> InvokeAsync(
        string endpoint,
        MessageBuffer? payload = null,
        InvokeOptions options = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Registers a service by providing a name and handler.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="handler"></param>
    /// <param name="descriptor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask RegisterServiceAsync(
        string endpoint,
        MessageHandler handler,
        EndpointDescriptor? descriptor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a service registration.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Registers an endpoint by providing a name, handler and optional descriptor.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="handler"></param>
    /// <param name="descriptor"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask RegisterEndpointAsync(
        string endpoint,
        MessageHandler handler,
        EndpointDescriptor? descriptor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes an endpoint registration.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask UnregisterEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
}