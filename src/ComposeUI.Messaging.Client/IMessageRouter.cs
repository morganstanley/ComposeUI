// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

namespace ComposeUI.Messaging.Client;

public interface IMessageRouter : IAsyncDisposable
{
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
    /// <param name="topicName"></param>
    /// <param name="observer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<IDisposable> SubscribeAsync(
        string topicName,
        IObserver<RouterMessage> observer,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes a message to a topic.
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask PublishAsync(string topicName, string? payload = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invokes a named service.
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<string?> InvokeAsync(
        string serviceName,
        string? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Registers a service by providing a name and handler.
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="handler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask RegisterServiceAsync(
        string serviceName,
        ServiceInvokeHandler handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a service registration.
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask UnregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}