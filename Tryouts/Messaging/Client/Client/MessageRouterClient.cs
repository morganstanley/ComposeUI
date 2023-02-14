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

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging.Client;

internal sealed class MessageRouterClient : IMessageRouter
{
    public MessageRouterClient(
        IConnection connection,
        MessageRouterOptions options,
        ILogger<MessageRouterClient>? logger = null)
    {
        _connection = connection;
        _options = options;
        _logger = logger ?? NullLogger<MessageRouterClient>.Instance;
    }

    public string? ClientId => _clientId;

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        switch (_connectionState)
        {
            case ConnectionState.Closed:
                throw ThrowHelper.ConnectionClosed();

            case ConnectionState.Connected:
                return default;

            case ConnectionState.Connecting:
                return new ValueTask(_connectTaskSource.Task);
        }

        return ConnectAsyncCore();
    }

    public ValueTask<IDisposable> SubscribeAsync(
        string topicName,
        ISubscriber<TopicMessage> subscriber,
        CancellationToken cancellationToken = default)
    {
        Topic.Validate(topicName);

        var needsSubscription = false;

        var topic = _topics.GetOrAdd(
            topicName,
            _ =>
            {
                needsSubscription = true;

                return new Topic<TopicMessage>(topicName, _logger);
            });

        return needsSubscription
            ? SubscribeCore(topicName, topic, subscriber, cancellationToken)
            : new ValueTask<IDisposable>(topic.Subscribe(subscriber));
    }

    public async ValueTask PublishAsync(
        string topic,
        MessageBuffer? payload = null,
        PublishOptions options = default,
        CancellationToken cancellationToken = default)
    {
        Topic.Validate(topic);

        await SendMessageAsync(
            new PublishMessage
            {
                Topic = topic,
                Payload = payload,
                Scope = options.Scope,
                CorrelationId = options.CorrelationId
            },
            cancellationToken);
    }

    public async ValueTask<MessageBuffer?> InvokeAsync(
        string endpoint,
        MessageBuffer? payload = null,
        InvokeOptions options = default,
        CancellationToken cancellationToken = default)
    {
        Endpoint.Validate(endpoint);

        var request = new InvokeRequest
        {
            RequestId = GenerateRequestId(),
            Endpoint = endpoint,
            Payload = payload,
            Scope = options.Scope,
            CorrelationId = options.CorrelationId,
        };

        var response = await SendRequestAsync(request, cancellationToken);

        return response.Payload;
    }

    public ValueTask RegisterServiceAsync(
        string endpoint,
        MessageHandler handler,
        EndpointDescriptor? descriptor = null,
        CancellationToken cancellationToken = default)
    {
        Endpoint.Validate(endpoint);

        try
        {
            if (!_endpointHandlers.TryAdd(endpoint, handler))
                throw ThrowHelper.DuplicateEndpoint(endpoint);

            return RegisterServiceCore(endpoint, descriptor, cancellationToken);
        }
        catch
        {
            _endpointHandlers.TryRemove(endpoint, out _);

            throw;
        }
    }

    public ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        if (!_endpointHandlers.TryRemove(endpoint, out _))
            return default;

        return UnregisterServiceCore(endpoint, cancellationToken);
    }

    public ValueTask RegisterEndpointAsync(
        string endpoint,
        MessageHandler handler,
        EndpointDescriptor? descriptor = null,
        CancellationToken cancellationToken = default)
    {
        Endpoint.Validate(endpoint);

        if (!_endpointHandlers.TryAdd(endpoint, handler))
            throw ThrowHelper.DuplicateEndpoint(endpoint);

        return ConnectAsync(cancellationToken);
    }

    public ValueTask UnregisterEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        _endpointHandlers.TryRemove(endpoint, out _);

        return default;
    }

    public async ValueTask DisposeAsync()
    {
        // TODO: Fail pending requests and complete subscribers
        using (await _mutex.LockAsync())
        {
            if (_connectionState != ConnectionState.Connected)
                return;

            _connectionState = ConnectionState.Closed;
            await _connection.DisposeAsync();
        }
    }

    private string? _clientId;
    private readonly IConnection _connection;
    private ConnectionState _connectionState;
    private readonly TaskCompletionSource _connectTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private AsyncLock _mutex = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AbstractResponse>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, MessageHandler> _endpointHandlers = new();
    private readonly ConcurrentDictionary<string, Topic<TopicMessage>> _topics = new();
    private readonly MessageRouterOptions _options;
    private readonly ILogger<MessageRouterClient> _logger;

    private async ValueTask ConnectAsyncCore()
    {
        using (await _mutex.LockAsync())
        {
            if (_connectionState == ConnectionState.Closed)
                throw ThrowHelper.ConnectionClosed();

            _connectionState = ConnectionState.Connecting;

            try
            {
                await _connection.ConnectAsync();
                _ = Task.Run(ReadMessagesAsync);

                await _connection.SendAsync(
                    new ConnectRequest
                    {
                        AccessToken = _options.AccessToken
                    });
            }
            catch (Exception e)
            {
                _connectionState = ConnectionState.Closed;
                _connectTaskSource.SetException(e);
            }
        }

        await _connectTaskSource.Task;
        _connectionState = ConnectionState.Connected;
    }

    private void HandleMessage(Message message)
    {
        /*
            Design notes
            
            While the public API supports async subscribers and other callbacks, 
            we avoid (make impossible) to block message processing by an async handler.
            In general, we process everything synchronously until the point where the
            actual user code is called, and we do not await it. Subscribers have their own
            async message queues so that they can't block each other. 
            This can lead to memory issues if a badly written subscriber can't process its messages
            fast enough, and its queue starts to grow infinitely. We might add some configuration
            options to fine-tune this behavior later, eg. set a max queue size (in that case, we
            must signal the subscriber in some way, possibly with a flag in MessageContext, or a
            dedicated callback).
            
         */

        switch (message)
        {
            case AbstractResponse response:
                HandleResponse(response);

                return;

            case { Type: MessageType.Topic }:
                HandleTopicMessage((Protocol.Messages.TopicMessage)message);

                return;

            case { Type: MessageType.Invoke }:
            {
                HandleInvokeRequest((InvokeRequest)message);

                return;
            }

            case { Type: MessageType.ConnectResponse }:
                HandleConnectResponse((ConnectResponse)message);

                return;
        }

        _logger.LogWarning("Unhandled message with type '{MessageType}'", message.Type);
    }

    private void HandleConnectResponse(ConnectResponse message)
    {
        if (message.Error != null)
        {
            _connectTaskSource.SetException(new MessageRouterException(message.Error));
        }
        else
        {
            _clientId = message.ClientId;
            _connectTaskSource.SetResult();
        }
    }

    private async void HandleInvokeRequest(InvokeRequest message)
    {
        await Task.Yield(); // Unblock the non-awaiting caller

        try
        {
            try
            {
                if (!_endpointHandlers.TryGetValue(message.Endpoint, out var handler))
                    throw ThrowHelper.UnknownEndpoint(message.Endpoint);

                var response = await handler(
                    message.Endpoint,
                    message.Payload,
                    new MessageContext
                    {
                        SourceId = message.SourceId!,
                        Scope = message.Scope,
                        CorrelationId = message.CorrelationId,
                    });

                await SendMessageAsync(
                    new InvokeResponse
                    {
                        RequestId = message.RequestId,
                        Payload = response,
                    },
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                await SendMessageAsync(
                    new InvokeResponse
                    {
                        RequestId = message.RequestId,
                        Error = new Error(e),
                    },
                    CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unhandled exception while processing an {nameof(InvokeRequest)}: {{ExceptionMessage}}", e.Message);
        }
    }

    private void HandleResponse(AbstractResponse message)
    {
        if (!_pendingRequests.TryRemove(message.RequestId, out var tcs))
            return;

        if (message.Error != null)
        {
            tcs.SetException(new MessageRouterException(message.Error));
        }
        else
        {
            tcs.SetResult(message);
        }
    }

    private void HandleTopicMessage(Protocol.Messages.TopicMessage message)
    {
        if (!_topics.TryGetValue(message.Topic, out var topic))
            return;

        var topicMessage = new TopicMessage(
            message.Topic,
            message.Payload,
            new MessageContext
            {
                SourceId = message.SourceId,
                Scope = message.Scope,
                CorrelationId = message.CorrelationId
            });

        topic.OnNext(topicMessage);
    }

    private string GenerateRequestId() => Guid.NewGuid().ToString("N");

    private async Task<TResponse> SendRequestAsync<TResponse>(
        AbstractRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TResponse : AbstractResponse
    {
        var tcs = new TaskCompletionSource<AbstractResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests.TryAdd(request.RequestId, tcs);

        try
        {
            await SendMessageAsync(request, cancellationToken);
        }
        catch (Exception e)
        {
            _pendingRequests.TryRemove(request.RequestId, out _);
            tcs.SetException(e);
        }

        return (TResponse)await tcs.Task;
    }

    private async Task SendMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);
        await _connection.SendAsync(message, cancellationToken);
    }

    private async void ReadMessagesAsync()
    {
        try
        {
            while (_connectionState != ConnectionState.Closed)
            {
                var message = await _connection.ReceiveAsync();

                if (_connectionState == ConnectionState.Closed)
                    break;

                HandleMessage(message);
            }

            _logger.LogInformation("Connection closed, exiting read loop");
        }
        catch (Exception e)
        {
            // TODO: Call OnError on the subscribers

            _logger.LogError(
                e,
                "Exception thrown while reading messages from the connection: {ExceptionMessage}",
                e.Message);
        }

        // TODO: Call OnCompleted on the subscribers
    }

    private async ValueTask RegisterServiceCore(
        string serviceName,
        EndpointDescriptor? descriptor,
        CancellationToken cancellationToken)
    {
        var request = new RegisterServiceRequest
        {
            RequestId = GenerateRequestId(),
            Endpoint = serviceName,
            Descriptor = descriptor,
        };

        await SendRequestAsync(request, cancellationToken);
    }

    private async ValueTask<IDisposable> SubscribeCore(
        string topicName,
        Topic<TopicMessage> topic,
        ISubscriber<TopicMessage> subscriber,
        CancellationToken cancellationToken)
    {
        var subscription = topic.Subscribe(subscriber);

        try
        {
            await SendMessageAsync(
                new SubscribeMessage
                {
                    Topic = topicName,
                },
                cancellationToken);

            return subscription;
        }
        catch
        {
            subscription.Dispose();

            throw;
        }
    }

    private async ValueTask UnregisterServiceCore(string serviceName, CancellationToken cancellationToken)
    {
        var request = new UnregisterServiceRequest
        {
            RequestId = GenerateRequestId(),
            Endpoint = serviceName,
        };

        await SendRequestAsync(request, cancellationToken);
    }

    private enum ConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Closed
    }

    // TODO: This should be something like an AsyncSubject once that is standardized and available in Rx.NET
    private class Topic<T>
    {
        public string Name { get; }

        public Topic(string name, ILogger logger)
        {
            Name = name;
            _logger = logger;
        }

        public IDisposable Subscribe(ISubscriber<T> subscriber)
        {
            var subscription = new Subscription(this, subscriber, _logger);
            _subscriptions = _subscriptions.Add(subscription);

            return subscription;
        }

        private void Unsubscribe(Subscription subscription)
        {
            _subscriptions = _subscriptions.Remove(subscription);
        }

        public void OnNext(T value)
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                subscription.OnNext(value);
            }
        }

        public void OnError(Exception exception)
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                subscription.OnError(exception);
            }
        }

        public void Complete()
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                subscription.Complete();
            }
        }

        private readonly ILogger _logger;
        private ImmutableList<Subscription> _subscriptions = ImmutableList<Subscription>.Empty;

        private ImmutableList<Subscription> GetSubscriptions()
        {
            return _subscriptions;
        }

        private class Subscription : IDisposable
        {
            public Subscription(Topic<T> topic, ISubscriber<T> subscriber, ILogger logger)
            {
                _subscriber = subscriber;
                _topic = topic;
                _logger = logger;
                ProcessMessages();
            }

            public void Dispose()
            {
                _topic.Unsubscribe(this);
            }

            public void OnNext(T value)
            {
                _queue.Writer.TryWrite(value); // Since the queue is unbounded, this will always succeed
            }

            public void OnError(Exception exception)
            {
                _queue.Writer.TryComplete(exception);
            }

            public void Complete()
            {
                _queue.Writer.TryComplete();
            }

            private async void ProcessMessages()
            {
                try
                {
                    while (await _queue.Reader.WaitToReadAsync())
                    {
                        while (_queue.Reader.TryRead(out var value))
                        {
                            try
                            {
                                await _subscriber.OnNextAsync(value);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(
                                    e,
                                    $"Exception thrown while invoking {nameof(ISubscriber<T>.OnNextAsync)} on a subscriber of topic '{{TopicName}}': {{ExceptionMessage}}",
                                    _topic.Name,
                                    e.Message);
                            }
                        }
                    }

                    try
                    {
                        await _subscriber.OnCompletedAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            e,
                            $"Exception thrown while invoking {nameof(ISubscriber<T>.OnCompletedAsync)} on a subscriber of topic '{{TopicName}}': {{ExceptionMessage}}",
                            _topic.Name,
                            e.Message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Exception thrown while processing messages for a subscriber of topic '{Topic}': {ExceptionMessage}",
                        _topic.Name,
                        e.Message);

                    try
                    {
                        await _subscriber.OnErrorAsync(e is ChannelClosedException ? e.InnerException ?? e : e);
                    }
                    catch (Exception e2)
                    {
                        _logger.LogError(
                            e2,
                            $"Exception thrown while invoking {nameof(ISubscriber<T>.OnErrorAsync)} on a subscriber of topic '{{TopicName}}': {{ExceptionMessage}}",
                            _topic.Name,
                            e2.Message);
                    }
                }
            }

            private readonly ISubscriber<T> _subscriber;

            private readonly Channel<T> _queue = Channel.CreateUnbounded<T>(
                new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = false
                });

            private readonly Topic<T> _topic;
            private readonly ILogger _logger;
        }
    }
}
