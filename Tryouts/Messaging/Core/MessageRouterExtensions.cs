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

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
///     Static extension methods for <see cref="IMessageRouter" />
/// </summary>
public static class MessageRouterExtensions
{
    /// <inheritdoc cref="IMessageRouter.PublishAsync"/>
    public static ValueTask PublishAsync(
        this IMessageRouter messageRouter,
        string topic,
        string payload,
        PublishOptions options = default,
        CancellationToken cancellationToken = default)
    {
        return messageRouter.PublishAsync(
            topic,
            MessageBuffer.Create(payload),
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="IMessageRouter.InvokeAsync"/>
    public static async ValueTask<string?> InvokeAsync(
        this IMessageRouter messageRouter,
        string endpoint,
        string? payload,
        InvokeOptions options = default,
        CancellationToken cancellationToken = default)
    {
        var response = await messageRouter.InvokeAsync(
            endpoint,
            payload == null ? null : MessageBuffer.Create(payload),
            options,
            cancellationToken);

        return response?.GetString();
    }

    /// <inheritdoc cref="IMessageRouter.RegisterServiceAsync"/>
    public static ValueTask RegisterServiceAsync(
        this IMessageRouter messageRouter,
        string endpoint,
        PlainTextMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        return messageRouter.RegisterServiceAsync(
            endpoint,
            // ReSharper disable once VariableHidesOuterVariable
            async (endpoint, payload, context) =>
            {
                var response = await handler(endpoint, payload?.GetString(), context);

                return response == null ? null : MessageBuffer.Create(response);
            },
            cancellationToken);
    }

    /// <summary>
    ///     Subscribes to a topic with an <see cref="IObserver{T}"/> that receives the full <see cref="TopicMessage"/>.
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topic"></param>
    /// <param name="observer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IDisposable> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topic,
        IObserver<TopicMessage> observer,
        CancellationToken cancellationToken = default)
    {
        var innerSubscriber = Subscriber.Create<TopicMessage>(
            message =>
            {
                observer.OnNext(message);

                return default;

            },
            exception =>
            {
                observer.OnError(exception);

                return default;
            },
            () =>
            {
                observer.OnCompleted();

                return default;
            });

        return messageRouter.SubscribeAsync(topic, innerSubscriber, cancellationToken);
    }

    /// <summary>
    ///     Subscribes to a topic with an <see cref="IObserver{T}"/> that receives the string payload instead of a
    ///     <see cref="TopicMessage" />
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topic"></param>
    /// <param name="observer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IDisposable> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topic,
        IObserver<string?> observer,
        CancellationToken cancellationToken = default)
    {
        var innerSubscriber = Subscriber.Create<TopicMessage>(
            message =>
            {
                observer.OnNext(message.Payload?.GetString());

                return default;

            },
            exception =>
            {
                observer.OnError(exception);

                return default;
            },
            () =>
            {
                observer.OnCompleted();

                return default;
            });

        return messageRouter.SubscribeAsync(topic, innerSubscriber, cancellationToken);
    }

    /// <summary>
    ///     Subscribes to a topic with a subscriber that receives the string payload instead of a
    ///     <see cref="TopicMessage" />
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topic"></param>
    /// <param name="subscriber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<IDisposable> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topic,
        ISubscriber<string?> subscriber,
        CancellationToken cancellationToken = default)
    {
        var innerSubscriber = Subscriber.Create<TopicMessage>(
            message => subscriber.OnNextAsync(message.Payload?.GetString()),
            subscriber.OnErrorAsync,
            subscriber.OnCompletedAsync);

        return messageRouter.SubscribeAsync(topic, innerSubscriber, cancellationToken);
    }
    
    /// <summary>
    /// Subscribes to a topic and returns an <see cref="IAsyncEnumerable{T}"/> that can be used to asynchronously iterate the messages.
    /// </summary>
    /// <param name="messageRouter"></param>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<TopicMessage> SubscribeAsync(
        this IMessageRouter messageRouter,
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<TopicMessage>();

        using var subscription = await messageRouter.SubscribeAsync(
            topic,
            Subscriber.Create<TopicMessage>(
                onNext: message => channel.Writer.WriteAsync(message, cancellationToken),
                onError: exception =>
                {
                    channel.Writer.TryComplete(exception);

                    return default;
                },
                onCompleted: () =>
                {
                    channel.Writer.TryComplete();

                    return default;
                }),
            cancellationToken);

        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            yield return message;
        }
    }
}
