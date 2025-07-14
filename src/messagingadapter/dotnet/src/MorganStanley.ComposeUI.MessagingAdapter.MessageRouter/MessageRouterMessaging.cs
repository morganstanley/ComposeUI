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
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

namespace MorganStanley.ComposeUI.MessagingAdapter;

/// <summary>
/// Provides an implementation of <see cref="IMessaging"/> that wraps an <see cref="IMessagingService"/> instance,
/// offering methods for connecting, invoking, publishing, subscribing, and service registration with consistent exception handling.
/// </summary>
/// <remarks>
/// This class adapts the lower-level <see cref="IMessagingService"/> interface to the <see cref="IMessaging"/> abstraction,
/// and ensures that exceptions from the message router are wrapped in adapter-specific exceptions.
/// </remarks>
internal class MessageRouterMessaging : IMessaging
{
    public string? ClientId { get; set; }
    private readonly IMessagingService _messagingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageRouterMessaging"/> class with the specified messaging service.
    /// </summary>
    /// <param name="messagingService">The underlying <see cref="IMessagingService"/> to be wrapped by this adapter.</param>
    public MessageRouterMessaging(IMessagingService messagingService)
    {
        _messagingService = messagingService;
    }

    /// <summary>
    /// Connects to the messaging service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask ConnectAsync(CancellationToken cancellationToken = default) => WrapMessageRouterExceptionsAsync(() => _messagingService.ConnectAsync(cancellationToken));

    /// <summary>
    /// Invokes an endpoint asynchronously and returns the response as a string.
    /// </summary>
    /// <param name="endpoint">The endpoint to invoke.</param>
    /// <param name="payload">The payload to send, or null.</param>
    /// <param name="options">Optional invoke options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation, with the response as a string.</returns>
    public ValueTask<string?> InvokeAsync(
        string endpoint,
        string? payload = null,
        Abstractions.InvokeOptions? options = null,
        CancellationToken cancellationToken = default) => WrapMessageRouterExceptionsAsync(async () =>
        {
            var buffer = await _messagingService.InvokeAsync(
                endpoint,
                MessageBuffer.Create(payload ?? "null"),
                new Messaging.Abstractions.InvokeOptions { CorrelationId = options?.CorrelationId },
                cancellationToken);
            return buffer?.GetString();
        });

    /// <summary>
    /// Publishes a message to the specified topic.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="message">The message to publish, or null.</param>
    /// <param name="options">Optional publish options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask PublishAsync(
        string topic,
        string? message = null,
        Abstractions.PublishOptions options = default,
        CancellationToken cancellationToken = default) => WrapMessageRouterExceptionsAsync(() =>
        _messagingService.PublishAsync(
            topic,
            MessageBuffer.Create(message ?? "null"),
            new Messaging.Abstractions.PublishOptions { CorrelationId = options.CorrelationId },
            cancellationToken));

    /// <summary>
    /// Registers a service endpoint with a subscriber callback.
    /// </summary>
    /// <param name="endpoint">The endpoint to register.</param>
    /// <param name="subscriber">The callback to invoke when a message is received.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask RegisterServiceAsync(
        string endpoint,
        Func<string, string, MessageAdapterContext?, ValueTask<string>> subscriber,
        CancellationToken cancellationToken = default) => WrapMessageRouterExceptionsAsync(() =>
        _messagingService.RegisterServiceAsync(endpoint, async (name, buffer, context) =>
        {
            var payload = buffer?.GetString() ?? string.Empty;
            // Map Messaging.Abstractions.MessageContext to MessagingAdapter.Abstractions.MessageAdapterContext
            var adapterContext = context is null
                ? null
                : new MessageAdapterContext
                {
                    CorrelationId = context.CorrelationId
                    // Map other properties if needed
                };
            var result = await subscriber(name, payload, adapterContext);
            return string.IsNullOrEmpty(result) ? null : MessageBuffer.Create(result);
        }, cancellationToken));

    /// <summary>
    /// Subscribes to the specified topic and invokes the subscriber callback when a message is received.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <param name="subscriber">The callback to invoke when a message is received.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{IDisposable}"/> representing the asynchronous subscription operation.
    /// The result contains an <see cref="IDisposable"/> that can be used to unsubscribe.
    /// </returns>
    public async ValueTask<IAsyncDisposable> SubscribeAsync(
        string topic,
        Func<string, ValueTask> subscriber,
        CancellationToken cancellationToken = default)
    {
        return await WrapMessageRouterExceptionsAsync(async () =>
        {
            var asyncSubscriber = new Func<IMessageBuffer, ValueTask>(buffer =>
            {
                var message = buffer.GetString();
                return subscriber(message);
            });
            var asyncDisposable = await _messagingService.SubscribeAsync(topic, asyncSubscriber, cancellationToken);

            return asyncDisposable;
        });
    }

    /// <summary>
    /// Unregisters a previously registered service endpoint from the messaging service.
    /// </summary>
    /// <param name="endpoint">The endpoint to unregister.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await _messagingService.UnregisterServiceAsync(endpoint, cancellationToken);
    }

    /// <summary>
    /// Executes the specified asynchronous operation and wraps MessageRouter exceptions with MessagingAdapter exceptions.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    private static async ValueTask WrapMessageRouterExceptionsAsync(Func<ValueTask> operation)
    {
        try
        {
            await operation();
        }
        catch (MessageRouterDuplicateEndpointException ex)
        {
            throw new MessagingAdapterDuplicateEndpointException(ex.Name, ex.Message, ex);
        }
        catch (MessageRouterException ex)
        {
            throw new MessagingAdapterException(ex.Name, ex.Message, ex);
        }
    }

    /// <summary>
    /// Executes the specified asynchronous operation and wraps MessageRouter exceptions with MessagingAdapter exceptions.
    /// </summary>
    /// <typeparam name="TResult">The result type of the asynchronous operation.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <returns>A ValueTask representing the asynchronous operation with a result.</returns>
    private static async ValueTask<TResult> WrapMessageRouterExceptionsAsync<TResult>(Func<ValueTask<TResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (MessageRouterDuplicateEndpointException ex)
        {
            throw new MessagingAdapterDuplicateEndpointException(ex.Name, ex.Message, ex);
        }
        catch (MessageRouterException ex)
        {
            throw new MessagingAdapterException(ex.Name, ex.Message, ex);
        }
    }
}