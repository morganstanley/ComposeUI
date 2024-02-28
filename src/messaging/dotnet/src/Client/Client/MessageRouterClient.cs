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
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Instrumentation;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging.Client;

internal sealed class MessageRouterClient : IMessageRouter
{
    public MessageRouterClient(
        IConnectionFactory connectionFactory,
        MessageRouterOptions options,
        ILogger<MessageRouterClient>? logger = null)
    {
        _connection = connectionFactory.CreateConnection();
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

    public ValueTask<IAsyncDisposable> SubscribeAsync(
        string topic,
        IAsyncObserver<TopicMessage> subscriber,
        CancellationToken cancellationToken = default)
    {
        Protocol.Topic.Validate(topic);
        CheckState();

        return SubscribeAsyncCore(GetTopic(topic), subscriber, cancellationToken);
    }

    public async ValueTask PublishAsync(
        string topic,
        MessageBuffer? payload = null,
        PublishOptions options = default,
        CancellationToken cancellationToken = default)
    {
        Protocol.Topic.Validate(topic);

        await SendRequestAsync(
            new PublishMessage
            {
                RequestId = GenerateRequestId(),
                Topic = topic,
                Payload = payload,
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
        CheckState();

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

    internal IAsyncObservable<TopicMessage> GetTopicObservable(string topic)
    {
        Protocol.Topic.Validate(topic);
        CheckState();

        return GetTopic(topic);
    }

    private Topic GetTopic(string topicName)
    {
        return _topics.GetOrAdd(
            topicName,
            _ => new Topic(topicName, this, _logger));
    }

    private string? _clientId;
    private readonly IConnection _connection;
    private ConnectionState _connectionState;
    private readonly StateChangeEvents _stateChangeEvents = new();
    private readonly AsyncLock _mutex = new();

    private readonly Channel<MessageWrapper<Message, object?>> _sendChannel =
        Channel.CreateUnbounded<MessageWrapper<Message, object?>>(new UnboundedChannelOptions {SingleReader = true});

    private readonly ConcurrentDictionary<string, TaskCompletionSource<AbstractResponse>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, MessageHandler> _endpointHandlers = new();
    private readonly ConcurrentDictionary<string, Topic> _topics = new();
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
            OnConnectStart();
            await _connection.ConnectAsync(cancellationToken);

            _stateChangeEvents.SendReceiveCompleted = Task.WhenAll(SendMessagesAsync(), ReceiveMessagesAsync());

            await _sendChannel.Writer.WriteAsync(
                new MessageWrapper<Message, object?>(
                    new ConnectRequest {AccessToken = _options.AccessToken}),
                cancellationToken);

            await _stateChangeEvents.Connected.Task;
        }
        catch (MessageRouterException e)
        {
            throw;
        }
        catch (Exception e)
        {
            await CloseAsyncCore(e);
            OnConnectStop(e);

            throw ThrowHelper.ConnectionFailed(e);
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

            case {Type: MessageType.Topic}:
                HandleTopicMessage((Protocol.Messages.TopicMessage) message);

                return;

            case {Type: MessageType.Invoke}:
            {
                HandleInvokeRequest((InvokeRequest) message);

                return;
            }

            case {Type: MessageType.ConnectResponse}:
                HandleConnectResponse((ConnectResponse) message);

                return;
        }

        _logger.LogWarning("Unhandled message with type '{MessageType}'", message.Type);
    }

    private void HandleConnectResponse(ConnectResponse message)
    {
        _ = Task.Factory.StartNew(
            async () =>
            {
                using (await _mutex.LockAsync())
                {
                    if (message.Error != null)
                    {
                        _connectionState = ConnectionState.Closed;
                        var exception = new MessageRouterException(message.Error);
                        _stateChangeEvents.Connected.TrySetException(exception);
                        OnCloseStart();
                        OnCloseStop(exception);
                        OnConnectStop(exception);
                    }
                    else
                    {
                        _clientId = message.ClientId;
                        _connectionState = ConnectionState.Connected;
                        _stateChangeEvents.Connected.TrySetResult();
                        OnConnectStop();
                    }
                }
            },
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void HandleInvokeRequest(InvokeRequest message)
    {
        OnRequestStart(message);

        _ = Task.Factory.StartNew(
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
                        (state, exception) => OnRequestStop((Message) state!, exception),
                        message,
                        _stateChangeEvents.CloseRequested.Token);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        $"Unhandled exception while processing an {nameof(InvokeRequest)}: {{ExceptionMessage}}",
                        e.Message);
                    OnRequestStop(message, e);
                }
            },
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private void HandleResponse(AbstractResponse message)
    {
        if (!_pendingRequests.TryRemove(message.RequestId, out var tcs))
            return;

        if (message.Error != null)
        {
            var exception = new MessageRouterException(message.Error);
            tcs.TrySetException(exception);
        }
        else
        {
            tcs.TrySetResult(message);
        }
    }

    private void HandleTopicMessage(Protocol.Messages.TopicMessage message)
    {
        OnRequestStart(message);

        if (!_topics.TryGetValue(message.Topic, out var topic))
        {
            OnRequestStop(message);

            return;
        }

        var topicMessage = new TopicMessage(
            message.Topic,
            message.Payload,
            new MessageContext
            {
                SourceId = message.SourceId,
                CorrelationId = message.CorrelationId
            });

        var wrapper = new MessageWrapper<TopicMessage, Protocol.Messages.TopicMessage>(
            topicMessage,
            OnRequestStop,
            message);

        try
        {
            if (!topic.OnNext(wrapper))
            {
                OnRequestStop(message);
            }
        }
        catch (Exception e)
        {
            OnRequestStop(message, e);

            throw;
        }
    }

    private string GenerateRequestId() => Guid.NewGuid().ToString("N");

    private async Task<TResponse> SendRequestAsync<TResponse>(
        AbstractRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TResponse : AbstractResponse
    {
        CheckNotOnMainThread();

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
            tcs.TrySetException(e);
        }

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return (TResponse) await tcs.Task;
    }

    private ValueTask SendMessageAsync(Message message, CancellationToken cancellationToken)
    {
        return SendMessageAsync(message, onDequeued: null, state: null, cancellationToken);
    }

    private async ValueTask SendMessageAsync(
        Message message,
        Action<object?, Exception?>? onDequeued,
        object state,
        CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        var wrapper = new MessageWrapper<Message, object?>(message, onDequeued, state);
        wrapper.OnQueued();

        try
        {
            await _sendChannel.Writer.WriteAsync(
                wrapper,
                cancellationToken);
        }
        catch (Exception e)
        {
            wrapper.OnDequeued(e);

            throw;
        }
    }

    private async Task SendMessagesAsync()
    {
        try
        {
            while (await _sendChannel.Reader.WaitToReadAsync(_stateChangeEvents.CloseRequested.Token))
            {
                while (!_stateChangeEvents.CloseRequested.IsCancellationRequested
                       && _sendChannel.Reader.TryRead(out var wrapper))
                {
                    await _connection.SendAsync(wrapper.Message, _stateChangeEvents.CloseRequested.Token);
                    OnMessageSent(wrapper.Message);
                    wrapper.OnDequeued();
                }
            }
        }
        catch (OperationCanceledException e) when (e.CancellationToken == _stateChangeEvents.CloseRequested.Token)
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
                OnMessageReceived(message);
                HandleMessage(message);
            }
        }
        catch (OperationCanceledException e) when (e.CancellationToken == _stateChangeEvents.CloseRequested.Token)
        {
        }
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

            RequestClose(ThrowHelper.ConnectionAborted(e));
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

    private async ValueTask<IAsyncDisposable> SubscribeAsyncCore(
        Topic topic,
        IAsyncObserver<TopicMessage> subscriber,
        CancellationToken cancellationToken)
    {
        var subscribeResult = topic.Subscribe(subscriber);

        if (!subscribeResult.NeedsSubscription) return subscribeResult.Subscription;

        try
        {
            await SendRequestAsync(
                new SubscribeMessage
                {
                    RequestId = GenerateRequestId(),
                    Topic = topic.Name
                },
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            return subscribeResult.Subscription;
        }
        catch
        {
            await subscribeResult.Subscription.DisposeAsync();

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
        _ = Task.Factory.StartNew(
            () => CloseAsyncCore(exception).AsTask(),
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private ValueTask CloseAsync(Exception? exception)
    {
        switch (_connectionState)
        {
            case ConnectionState.Closed:
                return default;
            case ConnectionState.Closing:
                return new ValueTask(_stateChangeEvents.Closed.Task);
        }

        return CloseAsyncCore(exception);
    }

    private async ValueTask CloseAsyncCore(Exception? exception)
    {
        ConnectionState oldState;

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

        using (await _mutex.LockAsync())
        {
            switch (oldState = _connectionState)
            {
                case ConnectionState.NotConnected:
                {
                    _connectionState = ConnectionState.Closed;
                    await _connection.DisposeAsync();

                    return;
                }

                case ConnectionState.Connecting:
                {
                    _connectionState = ConnectionState.Closed;
                    _stateChangeEvents.Connected.TrySetException(exception);

                    break;
                }

                case ConnectionState.Closing:
                    break;

                case ConnectionState.Closed:
                    return;

                case ConnectionState.Connected:
                {
                    _connectionState = ConnectionState.Closing;

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        switch (oldState)
        {
            case ConnectionState.Closing:
            {
                await _stateChangeEvents.Closed.Task;

                return;
            }
        }

        OnCloseStart();

        try
        {
            FailPendingRequests(exception);
            FailSubscribers(exception);
            await CloseTopics();
            _stateChangeEvents.CloseRequested.Cancel();
            await _stateChangeEvents.SendReceiveCompleted!;

            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown when closing the connection: {ExceptionMessage}", e.Message);
            }

            using (await _mutex.LockAsync())
            {
                _connectionState = ConnectionState.Closed;
            }

            OnCloseStop();
        }
        catch (Exception e)
        {
            OnCloseStop(e);

            throw;
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
        }

        async Task CloseTopics()
        {
            await Task.WhenAll(_topics.Select(t => t.Value.CloseAsync()));
            _topics.Clear();
        }
    }

    private async ValueTask TryUnsubscribe(Topic topic)
    {
        var requestId = GenerateRequestId();

        try
        {
            if (topic.CanUnsubscribe)
            {
                await SendRequestAsync(new UnsubscribeMessage { RequestId = requestId, Topic = topic.Name }, CancellationToken.None);
            }
        }
        catch (MessageRouterException exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, $"Exception thrown while unsubscribing, topic: {topic.Name}, request id: {requestId}.");
            }
        }
    }

    private void OnConnectStart()
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.ConnectStart))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.ConnectStart,
                new MessageRouterEvent(this, MessageRouterEventTypes.ConnectStart));
        }
    }

    private void OnConnectStop(Exception? exception = null)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.ConnectStop))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.ConnectStop,
                new MessageRouterEvent(this, MessageRouterEventTypes.ConnectStop, Exception: exception));
        }
    }

    private void OnMessageReceived(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageReceived))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageReceived,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageReceived, message));
        }
    }

    private void OnMessageSent(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageSent))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageSent,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent, message));
        }
    }

    private void OnRequestStart(Message message, Exception? exception = null)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.RequestStart))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.RequestStart,
                new MessageRouterEvent(this, MessageRouterEventTypes.RequestStart, message, exception));
        }
    }

    private void OnRequestStop(Message message, Exception? exception = null)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.RequestStop))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.RequestStop,
                new MessageRouterEvent(this, MessageRouterEventTypes.RequestStop, message, exception));
        }
    }

    private void OnCloseStart()
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.CloseStart))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.CloseStart,
                new MessageRouterEvent(this, MessageRouterEventTypes.CloseStart));
        }
    }

    private void OnCloseStop(Exception? exception = null)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.CloseStop))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.CloseStop,
                new MessageRouterEvent(this, MessageRouterEventTypes.CloseStop, Exception: exception));
        }
    }

    [DebuggerStepThrough]
    private static void CheckNotOnMainThread()
    {
#if DEBUG
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            throw new InvalidOperationException(
                "The current thread is the main thread. Awaiting the resulting Task can cause a deadlock.");
        }
#endif
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
        public readonly TaskCompletionSource Connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly CancellationTokenSource CloseRequested = new();
        public readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? SendReceiveCompleted;
    }

    private sealed class MessageWrapper<TMessage, TState>
    {
        public MessageWrapper(
            TMessage message,
            Action<TState, Exception?>? onDequeued = null,
            TState state = default!)
        {
            Message = message;
            _state = state;
            _onDequeued = onDequeued;
        }

        public TMessage Message { get; }

        internal void OnQueued()
        {
            Interlocked.Increment(ref _queuedCount);
        }

        internal void OnDequeued(Exception? exception = null)
        {
            if (Interlocked.Decrement(ref _queuedCount) == 0)
            {
                _onDequeued?.Invoke(_state, exception);
            }
        }

        private readonly TState _state;
        private readonly Action<TState, Exception?>? _onDequeued;
        private int _queuedCount;
    }

    private class Topic : IAsyncObservable<TopicMessage>
    {
        public Topic(string name, MessageRouterClient messageRouter, ILogger logger)
        {
            Name = name;
            _messageRouter = messageRouter;
            _logger = logger;
        }

        public string Name { get; }

        public bool CanUnsubscribe
        {
            get
            {
                lock (_mutex)
                {
                    return _subscriptions.Count == 0;
                }
            }
        }

        public SubscribeResult Subscribe(IAsyncObserver<TopicMessage> subscriber)
        {
            lock (_mutex)
            {
                if (_exception != null)
                    throw _exception;

                if (_isCompleted)
                    throw ThrowHelper.ConnectionClosed();

                var needsSubscription = _subscriptions.Count == 0;
                var subscription = new Subscription(this, subscriber, _logger);
                _subscriptions.Add(subscription);
                _subscriberCount.AddCount();

                return new SubscribeResult(subscription, needsSubscription);
            }
        }

        public bool OnNext(MessageWrapper<TopicMessage, Protocol.Messages.TopicMessage> value)
        {
            lock (_mutex)
            {
                if (_isCompleted || _subscriptions.Count == 0)
                    return false;

                foreach (var subscription in _subscriptions)
                {
                    subscription.OnNext(value);
                }

                return true;
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

        public void Unsubscribe(Subscription subscription)
        {
            lock (_mutex)
            {
                if (_isCompleted || !_subscriptions.Remove(subscription)) return;

                if (_subscriptions.Count == 0)
                {
                    Task.Factory.StartNew(
                        () => _messageRouter.TryUnsubscribe(this),
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(IAsyncObserver<TopicMessage> observer)
        {
            return _messageRouter.SubscribeAsyncCore(this, observer, CancellationToken.None);
        }

        public Task CloseAsync()
        {
            return _subscriberCount.WaitAsync();
        }

        public void OnSubscriberCompleted()
        {
            _subscriberCount.Signal();
        }

        private readonly MessageRouterClient _messageRouter;
        private readonly ILogger _logger;
        private readonly object _mutex = new();
        private readonly HashSet<Subscription> _subscriptions = new();
        private bool _isCompleted;
        private Exception? _exception;
        private AsyncCountdownEvent _subscriberCount = new(0);
    }

    private class Subscription : IAsyncDisposable
    {
        public Subscription(Topic topic, IAsyncObserver<TopicMessage> subscriber, ILogger logger)
        {
            _subscriber = subscriber;
            _topic = topic;
            _logger = logger;
            _ = Task.Factory.StartNew(ProcessMessages, TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void OnNext(MessageWrapper<TopicMessage, Protocol.Messages.TopicMessage> value)
        {
            // Note the order. If we only invoked OnQueued AFTER the TryWrite call,
            // a race condition would allow OnDequeued to be called in between,
            // resulting in a false ordering of events (the MessageRouterClient would signal OnMessageProcessed
            // before the subscriber actually receiving it).

            value.OnQueued();

            if (!_queue.Writer.TryWrite(value))
            {
                value.OnDequeued();
            }
        }

        public void OnError(Exception exception)
        {
            _queue.Writer.TryComplete(exception);
        }

        public ValueTask DisposeAsync()
        {
            return InvokeLocked(
                _ =>
                {
                    if (_disposed)
                        return ValueTask.CompletedTask;

                    _disposed = true;
                    _queue.Writer.TryComplete();
                    _topic.Unsubscribe(this);

                    return ValueTask.CompletedTask;
                },
                (object?) null);
        }

        private async Task ProcessMessages()
        {
            try
            {
                while (await _queue.Reader.WaitToReadAsync())
                {
                    using (await _lock.LockAsync())
                    {
                        // We have to empty the queue even if already unsubscribed, to make sure OnDequeued is called on every message

                        while (_queue.Reader.TryRead(out var value))
                        {
                            await InvokeSubscriber(_subscriber.OnNextAsync, value.Message, nameof(_subscriber.OnNextAsync));
                            value.OnDequeued();
                        }
                    }
                }

                using (await _lock.LockAsync())
                {
                    await InvokeSubscriber(_ => _subscriber.OnCompletedAsync(), (object?)null, nameof(_subscriber.OnCompletedAsync));
                }
            }
            catch (Exception e)
            {
                using (await _lock.LockAsync())
                {
                    await InvokeSubscriber(
                        _subscriber.OnErrorAsync,
                        e is ChannelClosedException ? e.InnerException ?? e : e,
                        nameof(_subscriber.OnErrorAsync));
                }
            }
            finally
            {
                _topic.OnSubscriberCompleted();
            }
        }

        private ValueTask InvokeLocked<TArg>(Func<TArg, ValueTask> action, TArg arg)
        {
            return _recursion.Value == 0 
                ? InvokeLockedImpl(action, arg) 
                : action(arg);

            async ValueTask InvokeLockedImpl<TArg>(Func<TArg, ValueTask> action, TArg arg)
            {
                using (await _lock.LockAsync())
                {

                    await action(arg);
                }
            }
        }

        private async ValueTask InvokeSubscriber<TArg>(Func<TArg, ValueTask> action, TArg arg, string methodName)
        {
            if (_disposed) return;

            _recursion.Value += 1;
            try
            {
                await action(arg);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    $"Exception thrown while invoking {{MethodName}} on a subscriber of topic '{{TopicName}}': {{ExceptionMessage}}",
                    methodName,
                    _topic.Name,
                    e.Message);
            }
            finally
            {
                _recursion.Value -= 1;
            }
        }

        private readonly IAsyncObserver<TopicMessage> _subscriber;

        private readonly Channel<MessageWrapper<TopicMessage, Protocol.Messages.TopicMessage>> _queue =
            Channel.CreateUnbounded<MessageWrapper<TopicMessage, Protocol.Messages.TopicMessage>>(
                new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = true
                });

        private readonly Topic _topic;
        private readonly ILogger _logger;
        private readonly AsyncLock _lock = new();
        private readonly AsyncLocal<int> _recursion = new(); // Recursion counter when invoking a method of the subscriber
        private bool _disposed;
    }

    private record struct SubscribeResult(Subscription Subscription, bool NeedsSubscription);
}