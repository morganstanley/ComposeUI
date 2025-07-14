﻿// Morgan Stanley makes this available to you under the Apache License,
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

namespace MorganStanley.ComposeUI.Messaging.Abstractions;

public interface IMessagingService : IAsyncDisposable
{
    /// <summary>
    /// Gets the client ID of the current connection.
    /// </summary>
    /// <remarks>
    /// The returned value will be <value>null</value> if the client is not connected.
    /// </remarks>
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
    /// Gets an observable that represents a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IAsyncDisposable> SubscribeAsync(string topic, 
        Func<IMessageBuffer, ValueTask> subscriber, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask PublishAsync(string topic, 
        IMessageBuffer? message = null, 
        PublishOptions optinos = default, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a service by providing a name and handler.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask RegisterServiceAsync(string endpoint,
        Func<string, IMessageBuffer?, MessageContext?, ValueTask<IMessageBuffer?>> subscriber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a service registration.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invokes a named service.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<IMessageBuffer?> InvokeAsync(
        string endpoint,
        IMessageBuffer? payload = null,
        InvokeOptions options = default,
        CancellationToken cancellationToken = default);
}