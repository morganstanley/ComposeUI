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
using System.Runtime.ExceptionServices;
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
            case ConnectionState.Connected:
                return default;

            case ConnectionState.Connecting:
                return new ValueTask(_stateChangeEvents.Connected.Task);

            case ConnectionState.Closing:
            case ConnectionState.Closed:
                throw ThrowHelper.ConnectionClosed();
        }

        return ConnectAsyncCore(cancellationToken);
    }

    public ValueTask<IDisposable> SubscribeAsync(
        string topicName,
        ISubscriber<TopicMessage> subscriber,
        CancellationToken cancellationToken = default)
    {
        Topic.Validate(topicName);
        CheckState();

        var needsSubscription = false;

        var topic = _topics.GetOrAdd(
            topicName,
            _ =>
            {
                needsSubscription = true;
                return new Topic<TopicMessage>(topicName, _logger);
            });

        return needsSubscription
            ? SubscribeAsyncCore(topicName, topic, subscriber, cancellationToken)
            : ValueTask.FromResult(topic.Subscribe(subscriber));
    }

    public ValueTask PublishAsync(
        string topic,
        MessageBuffer? payload = null,
        PublishOptions options = default,
        CancellationToken cancellationToken = default)
    {
        Topic.Validate(topic);

        return SendMessageAsync(
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
        CheckState();

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
        CheckState();

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
        CheckState();

        if (!_endpointHandlers.TryAdd(endpoint, handler))
            throw ThrowHelper.DuplicateEndpoint(endpoint);

        return ConnectAsync(cancellationToken);
    }

    public ValueTask UnregisterEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        CheckState();
        _endpointHandlers.TryRemove(endpoint, out _);

        return default;
    }

    public ValueTask DisposeAsync()
    {
        return CloseAsync(null);
    }

    private string? _clientId;
    private readonly IConnection _connection;
    private ConnectionState _connectionState;
    private readonly StateChangeEvents _stateChangeEvents = new();
    private readonly AsyncLock _mutex = new();
    private readonly Channel<Message> _sendChannel = Channel.CreateUnbounded<Message>();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AbstractResponse>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, MessageHandler> _endpointHandlers = new();
    private readonly ConcurrentDictionary<string, Topic<TopicMessage>> _topics = new();
    private readonly MessageRouterOptions _options;
    private readonly ILogger<MessageRouterClient> _logger;

    private async ValueTask ConnectAsyncCore(CancellationToken cancellationToken)
    {
        ConnectionState oldState;

        using (await _mutex.LockAsync())
        {
            oldState = _connectionState;

            switch (_connectionState)
            {
                case ConnectionState.NotConnected:
                    _connectionState = ConnectionState.Connecting;

                    break;

                case ConnectionState.Connecting:
                    break;

                case ConnectionState.Connected:
                    return;

                case ConnectionState.Closing or ConnectionState.Closed:
                    throw ThrowHelper.ConnectionClosed();
            }
        }

        if (oldState == ConnectionState.Connecting)
        {
            await _stateChangeEvents.Connected.Task;

            return;
        }

        try
        {
            await _connection.ConnectAsync(cancellationToken);
            _stateChangeEvents.SendReceiveCompleted = Task.WhenAll(SendMessagesAsync(), ReceiveMessagesAsync());

            await _sendChannel.Writer.WriteAsync(
                new ConnectRequest { AccessToken = _options.AccessToken }, 
                cancellationToken);

            var connectResponse = await _stateChangeEvents.ConnectResponseReceived.Task.WaitAsync(cancellationToken);

            if (connectResponse.Error != null)
            {
                throw new MessageRouterException(connectResponse.Error);
            }

            using (await _mutex.LockAsync(cancellationToken))
            {
                _clientId = connectResponse.ClientId;
                _connectionState = ConnectionState.Connected;
                _stateChangeEvents.Connected.SetResult();
            }
        }
        catch (Exception e)
        {
            var exception = ThrowHelper.ConnectionFailed(e);
            ExceptionDispatchInfo.SetCurrentStackTrace(exception);
            await CloseAsync(exception);

            throw exception;
        }
    }

    private void CheckState()
    {
        switch (_connectionState)
        {
            case ConnectionState.Closing or ConnectionState.Closed:
                throw ThrowHelper.ConnectionClosed();
        }
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
        _stateChangeEvents.ConnectResponseReceived.SetResult(message);
    }

    private void HandleInvokeRequest(InvokeRequest message)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    InvokeResponse response;

                    try
                    {
                        if (!_endpointHandlers.TryGetValue(message.Endpoint, out var handler))
                            throw ThrowHelper.UnknownEndpoint(message.Endpoint);

                        // Last chance to back out if the connection was closed in the meantime
                        if (_connectionState != ConnectionState.Connected)
                            return;

                        var responsePayload = await handler(
                            message.Endpoint,
                            message.Payload,
                            new MessageContext
                            {
                                SourceId = message.SourceId!,
                                Scope = message.Scope,
                                CorrelationId = message.CorrelationId,
                            });

                        response = new InvokeResponse
                        {
                            RequestId = message.RequestId,
                            Payload = responsePayload
                        };
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            e,
                            "Exception thrown by the handler for endpoint '{Endpoint}': {ExceptionMessage}",
                            message.Endpoint,
                            e.Message);

                        response = new InvokeResponse
                        {
                            RequestId = message.RequestId,
                            Error = new Error(e),
                        };
                    }

                    await SendMessageAsync(
                        response,
                        _stateChangeEvents.CloseRequested.Token);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        $"Unhandled exception while processing an {nameof(InvokeRequest)}: {{ExceptionMessage}}",
                        e.Message);
                }
            });
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

        if (!_pendingRequests.TryAdd(request.RequestId, tcs))
        {
            throw new ArgumentException($"Duplicate {nameof(request.RequestId)}", nameof(request));
        }

        try
        {
            await SendMessageAsync(request, cancellationToken);
        }
        catch (Exception e)
        {
            _pendingRequests.TryRemove(request.RequestId, out _);
            tcs.SetException(e);
        }

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return (TResponse)await tcs.Task;
    }

    private async ValueTask SendMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await ConnectAsync(CancellationToken.None);
        await _sendChannel.Writer.WriteAsync(message, cancellationToken);
    }

    private async Task SendMessagesAsync()
    {
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(_stateChangeEvents.CloseRequested.Token))
            {
                while (_sendChannel.Reader.TryRead(out var message))
                {
                    await _connection.SendAsync(message, _stateChangeEvents.CloseRequested.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Exception thrown while sending messages to the connection: {ExceptionMessage}",
                e.Message);

            RequestClose(e);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        try
        {
            while (!_stateChangeEvents.CloseRequested.IsCancellationRequested)
            {
                var message = await _connection.ReceiveAsync(_stateChangeEvents.CloseRequested.Token);
                HandleMessage(message);
            }
        }
        catch (OperationCanceledException) { }
        catch (MessageRouterException e) when (e.Name is MessageRouterErrors.ConnectionClosed
                                                   or MessageRouterErrors.ConnectionAborted)
        {
            RequestClose(e);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Exception thrown while reading messages from the connection: {ExceptionMessage}",
                e.Message);

            RequestClose(e);
        }
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

    private async ValueTask<IDisposable> SubscribeAsyncCore(
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

    private void RequestClose(Exception? exception)
    {
        _ = Task.Run(() => CloseAsync(exception));
    }

    private async ValueTask CloseAsync(Exception? exception)
    {
        using (await _mutex.LockAsync())
        {
            if (_connectionState != ConnectionState.Connected)
                return;

            switch (exception)
            {
                case null:
                {
                    exception = ThrowHelper.ConnectionClosed();

                    break;
                }

                case MessageRouterException:
                    break;

                default:
                {
                    exception = ThrowHelper.ConnectionAborted(exception);

                    break;
                }
            }

            _connectionState = ConnectionState.Closing;
            FailPendingRequests(exception);
            FailSubscribers(exception);
        }

        _stateChangeEvents.CloseRequested.Cancel();
        await _stateChangeEvents.SendReceiveCompleted!;
        await _connection.DisposeAsync();

        using (await _mutex.LockAsync())
        {
            _connectionState = ConnectionState.Closed;
        }

        // ReSharper disable once VariableHidesOuterVariable
        void FailPendingRequests(Exception exception)
        {
            foreach (var request in _pendingRequests)
            {
                request.Value.TrySetException(exception);
            }

            _pendingRequests.Clear();
        }

        // ReSharper disable once VariableHidesOuterVariable
        void FailSubscribers(Exception exception)
        {
            foreach (var topic in _topics)
            {
                topic.Value.OnError(exception);
            }

            _topics.Clear();
        }
    }

    private enum ConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Closing,
        Closed
    }

    private class StateChangeEvents
    {
        public readonly TaskCompletionSource<ConnectResponse> ConnectResponseReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public readonly TaskCompletionSource Connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly CancellationTokenSource CloseRequested = new();
        public readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? SendReceiveCompleted;
    }

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
            lock (_mutex)
            {
                if (_exception != null)
                    throw _exception;

                if (_isCompleted)
                    throw ThrowHelper.ConnectionClosed();

                var subscription = new Subscription(this, subscriber, _logger);
                _subscriptions.Add(subscription);

                return subscription;
            }
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (_mutex)
            {
                _subscriptions.Remove(subscription);
            }
            // TODO: Unsubscribe from the topic completely if no more subscribers
        }

        public void OnNext(T value)
        {
            lock (_mutex)
            {
                if (_isCompleted)
                    return;

                foreach (var subscription in _subscriptions)
                {
                    subscription.OnNext(value);
                }
            }
        }

        public void OnError(Exception exception)
        {
            lock (_mutex)
            {
                if (_isCompleted)
                    return;

                _isCompleted = true;
                _exception = exception;
                
                foreach (var subscription in _subscriptions)
                {
                    subscription.OnError(exception);
                }
            }
        }

        public void Complete()
        {
            lock (_mutex)
            {
                if (_isCompleted)
                    return;

                _isCompleted = true;

                foreach (var subscription in _subscriptions)
                {
                    subscription.Complete();
                }
            }
        }

        private readonly ILogger _logger;
        private readonly object _mutex = new();
        private readonly List<Subscription> _subscriptions = new();
        private bool _isCompleted;
        private Exception? _exception;

        private class Subscription : IDisposable
        {
            public Subscription(Topic<T> topic, ISubscriber<T> subscriber, ILogger logger)
            {
                _subscriber = subscriber;
                _topic = topic;
                _logger = logger;
                _ = Task.Run(ProcessMessages);
            }

            public void Dispose()
            {
                _topic.Unsubscribe(this);
            }

            public void OnNext(T value)
            {
                _queue.Writer.TryWrite(value); // Since the queue is unbounded, this will succeed unless the channel was completed
            }

            public void OnError(Exception exception)
            {
                _queue.Writer.TryComplete(exception);
            }

            public void Complete()
            {
                _queue.Writer.TryComplete();
            }

            private async Task ProcessMessages()
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
