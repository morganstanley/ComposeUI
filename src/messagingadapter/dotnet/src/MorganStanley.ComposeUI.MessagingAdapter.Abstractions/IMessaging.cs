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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

/// <summary>
/// Defines the contract for messaging clients to interact with the Message Router server,
/// including connection management, service invocation, message publishing, subscription,
/// and service registration operations.
/// </summary>
public interface IMessaging
{
    /// <summary>
    /// Gets the unique identifier of the messaging client instance.
    /// </summary>
    public string? ClientId { get; }

    /// <summary>
    /// Asynchronously connects to the Message Router server endpoint.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>
    /// Clients don't need to call this method before calling other methods on this type.
    /// The client should automatically establish a connection when needed.
    /// </remarks>
    public ValueTask ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invokes a named service.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<string?> InvokeAsync(string endpoint, string? payload = null, InvokeOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask PublishAsync(string topic, string? message = null, PublishOptions options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a service by providing a name and handler.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask RegisterServiceAsync(string endpoint, Func<string, string, MessageAdapterContext?, ValueTask<string>> subscriber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an observable that represents a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IAsyncDisposable> SubscribeAsync(string topic, Func<string, ValueTask> subscriber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a service registration.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default);
}