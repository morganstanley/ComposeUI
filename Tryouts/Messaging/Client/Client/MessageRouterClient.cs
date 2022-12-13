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
        var needsSubscription = false;

        var topic = _topics.GetOrAdd(
            topicName,
            _ =>
            {
                needsSubscription = true;

                return new Topic<TopicMessage>();
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
        await ConnectAsync(cancellationToken);

        await _connection.SendAsync(
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
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests.TryAdd(requestId, tcs);
        await ConnectAsync(cancellationToken);

        try
        {
            await _connection.SendAsync(
                new InvokeRequest
                {
                    RequestId = requestId,
                    Endpoint = endpoint,
                    Payload = payload,
                    Scope = options.Scope,
                    CorrelationId = options.CorrelationId
                },
                cancellationToken);
        }
        catch (Exception e)
        {
            _pendingRequests.TryRemove(requestId, out _);
            tcs.SetException(e);
        }

        var response = (InvokeResponse)await tcs.Task;

        return response.Payload;
    }

    public ValueTask RegisterServiceAsync(
        string endpoint,
        MessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (!_serviceInvokeHandlers.TryAdd(endpoint, handler))
            throw new DuplicateServiceNameException();

        return RegisterServiceCore(endpoint);
    }

    public ValueTask UnregisterServiceAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        if (!_serviceInvokeHandlers.TryRemove(endpoint, out _))
            return default;

        return UnregisterServiceCore(endpoint, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
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
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, MessageHandler> _serviceInvokeHandlers = new();
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

    private Task HandleConnectResponse(ConnectResponse message)
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

        return Task.CompletedTask;
    }

    private async Task HandleInvokeRequest(InvokeRequest message)
    {
        try
        {
            if (!_serviceInvokeHandlers.TryGetValue(message.Endpoint, out var handler))
                throw new UnknownServiceException();

            var response = await handler(
                message.Endpoint,
                message.Payload,
                new MessageContext
                {
                    SourceId = message.SourceId!,
                    Scope = message.Scope,
                    CorrelationId = message.CorrelationId,
                });

            await ConnectAsync();

            await _connection.SendAsync(
                new InvokeResponse
                {
                    RequestId = message.RequestId,
                    Payload = response,
                });
        }
        catch (Exception e)
        {
            await ConnectAsync();

            await _connection.SendAsync(
                new InvokeResponse
                {
                    RequestId = message.RequestId,
                    Error = new Error(e),
                });
        }
    }

    private Task HandleInvokeResponse(InvokeResponse message)
    {
        if (!_pendingRequests.TryRemove(message.RequestId, out var tcs))
            return Task.CompletedTask;

        if (message.Error != null)
            tcs.SetException(new MessageRouterException(message.Error));
        else
            tcs.SetResult(message);

        return Task.CompletedTask;
    }

    private Task HandleMessage(Message message)
    {
        switch (message.Type)
        {
            case MessageType.ConnectResponse:
                return HandleConnectResponse((ConnectResponse)message);

            case MessageType.Topic:
                return HandleTopicMessage((Protocol.Messages.TopicMessage)message);

            case MessageType.RegisterServiceResponse:
                return HandleRegisterServiceResponse((RegisterServiceResponse)message);

            case MessageType.InvokeResponse:
                return HandleInvokeResponse((InvokeResponse)message);

            case MessageType.Invoke:
                return HandleInvokeRequest((InvokeRequest)message);
        }

        // TODO: log unhandled message
        return Task.CompletedTask;
    }

    private Task HandleRegisterServiceResponse(RegisterServiceResponse message)
    {
        if (!_pendingRequests.TryRemove(message.Endpoint, out var tcs))
            return Task.CompletedTask;

        if (message.Error != null)
        {
            _serviceInvokeHandlers.TryRemove(message.Endpoint, out _);
            tcs.SetException(new MessageRouterException(message.Error));
        }
        else
        {
            tcs.SetResult(message);
        }

        return Task.CompletedTask;
    }

    private async Task HandleTopicMessage(Protocol.Messages.TopicMessage message)
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

        await topic.OnNextAsync(topicMessage);
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

                await HandleMessage(message);
            }

            _logger.LogInformation("Connection closed, exiting read loop");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown while reading messages from the connection: {ExceptionMessage}", e.Message);
        }
    }

    private async ValueTask RegisterServiceCore(string serviceName)
    {
        await ConnectAsync();

        var tcs = _pendingRequests.GetOrAdd(
            serviceName,
            _ => new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously));

        await _connection.SendAsync(
            new RegisterServiceRequest
            {
                Endpoint = serviceName,
            });

        await tcs.Task;
    }

    private async ValueTask<IDisposable> SubscribeCore(
        string topicName,
        Topic<TopicMessage> topic,
        ISubscriber<TopicMessage> subscriber,
        CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        await _connection.SendAsync(
            new SubscribeMessage
            {
                Topic = topicName,
            },
            cancellationToken);

        return topic.Subscribe(subscriber);
    }

    private async ValueTask UnregisterServiceCore(string serviceName, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        await _connection.SendAsync(
            new UnregisterServiceMessage
            {
                Endpoint = serviceName,
            },
            cancellationToken);
    }

    private enum ConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Closed
    }

    // TODO: This should be an AsyncSubject once that is standardized and available in Rx.NET
    private class Topic<T>
    {
        public bool HasSubscribers => _subscriberCount > 0;

        public IDisposable Subscribe(ISubscriber<T> subscriber)
        {
            var subscription = new Subscription(this, subscriber);

            lock (_subscriptions)
            {
                _subscriptions.Add(subscription);
            }

            Interlocked.Increment(ref _subscriberCount);

            return subscription;
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (_subscriptions)
            {
                _subscriptions.Remove(subscription);
            }

            Interlocked.Decrement(ref _subscriberCount);
        }

        public async ValueTask OnNextAsync(T value)
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                // TODO: Decide how exceptions should be handled
                await subscription.Subscriber.OnNextAsync(value);
            }
        }

        public async ValueTask OnErrorAsync(Exception exception)
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                // TODO: Decide how exceptions should be handled
                await subscription.Subscriber.OnErrorAsync(exception);
            }
        }

        public async ValueTask CompleteAsync()
        {
            var subscriptions = GetSubscriptions();

            foreach (var subscription in subscriptions)
            {
                // TODO: Decide how exceptions should be handled
                await subscription.Subscriber.OnCompletedAsync();
            }
        }

        private readonly List<Subscription> _subscriptions = new();

        private int _subscriberCount;

        private Subscription[] GetSubscriptions()
        {
            lock (_subscriptions)
            {
                return _subscriptions.ToArray();
            }
        }

        private class Subscription : IDisposable
        {
            public Subscription(Topic<T> topic, ISubscriber<T> subscriber)
            {
                Subscriber = subscriber;
                _topic = topic;
            }

            public ISubscriber<T> Subscriber { get; }

            public void Dispose()
            {
                _topic.Unsubscribe(this);
            }

            private readonly Topic<T> _topic;
        }
    }

    private static class ThrowHelper
    {
        public static InvalidOperationException ConnectionClosed()
        {
            return new InvalidOperationException("The connection has been closed");
        }
    }
}
