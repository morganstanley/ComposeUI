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

using System.Reactive;
using System.Text;
using MorganStanley.ComposeUI.Messaging.Core;
using MorganStanley.ComposeUI.Messaging.Core.Serialization;

namespace MorganStanley.ComposeUI.Messaging.Client;

/// <summary>
///     Static extension methods for <see cref="IMessageRouter" />
/// </summary>
public static class MessageRouterExtensions
{
    /// <inheritdoc cref="IMessageRouter.PublishAsync"/>
    public static ValueTask PublishAsync(
        this IMessageRouter messageRouter,
        string topicName,
        string payload,
        CancellationToken cancellationToken = default)
    {
        return messageRouter.PublishAsync(
            topicName,
            Utf8Buffer.Create(payload),
            cancellationToken);
    }

    /// <inheritdoc cref="IMessageRouter.InvokeAsync"/>
    public static async ValueTask<string?> InvokeAsync(
        this IMessageRouter messageRouter,
        string serviceName,
        string? payload,
        CancellationToken cancellationToken = default)
    {
        var response = await messageRouter.InvokeAsync(
            serviceName,
            payload == null ? null : Utf8Buffer.Create(payload),
            cancellationToken);

        return response?.GetString();
    }

    /// <inheritdoc cref="IMessageRouter.RegisterServiceAsync"/>
    public static ValueTask RegisterServiceAsync(
        this IMessageRouter messageRouter,
        string serviceName,
        PlainTextServiceInvokeHandler handler,
        CancellationToken cancellationToken = default)
    {
        return messageRouter.RegisterServiceAsync(
            serviceName,
            // ReSharper disable once VariableHidesOuterVariable
            async (serviceName, payload) =>
            {
                var response = await handler(serviceName, payload?.GetString());

                return response == null ? null : Utf8Buffer.Create(response);
            },
            cancellationToken);
    }

    /// <summary>
    ///     Subscribes to a topic with an observer that receives the string payload instead of a
    ///     <see cref="RouterMessage" />
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topicName"></param>
    /// <param name="observer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IDisposable> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topicName,
        IObserver<string?> observer,
        CancellationToken cancellationToken = default)
    {
        var innerObserver = Observer.Create<RouterMessage>(
            message => observer.OnNext(message.Payload?.GetString()),
            observer.OnError,
            observer.OnCompleted);

        return messageRouter.SubscribeAsync(topicName, innerObserver, cancellationToken);
    }
}
