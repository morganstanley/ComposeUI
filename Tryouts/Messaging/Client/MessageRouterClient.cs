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
using System.Reactive.Subjects;
using MorganStanley.ComposeUI.Messaging.Client.Internal;
using MorganStanley.ComposeUI.Messaging.Client.Transport.Abstractions;
using MorganStanley.ComposeUI.Messaging.Core.Exceptions;
using MorganStanley.ComposeUI.Messaging.Core.Messages;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging.Client;

internal sealed class MessageRouterClient : IMessageRouter
{
    public MessageRouterClient(IConnection connection, MessageRouterOptions options)
    {
        _connection = connection;
        _options = options;
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
        IObserver<RouterMessage> observer,
        CancellationToken cancellationToken = default)
    {
        var needsSubscription = false;

        var topic = _subscriptions.GetOrAdd(
            topicName,
            _ =>
            {
                needsSubscription = true;

                return new Subject<RouterMessage>();
            });

        return needsSubscription
            ? SubscribeCore(topicName, topic, observer, cancellationToken)
            : new ValueTask<IDisposable>(topic.Subscribe(observer));
    }

    public async ValueTask PublishAsync(
        string topicName,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);
        await _connection.SendAsync(new PublishMessage(topicName, payload), cancellationToken);
    }

    public async ValueTask<string?> InvokeAsync(
        string serviceName,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<Message>();
        _pendingRequests.TryAdd(requestId, tcs);
        await ConnectAsync(cancellationToken);

        try
        {
            await _connection.SendAsync(new InvokeRequest(requestId, serviceName, payload), cancellationToken);
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
        string serviceName,
        ServiceInvokeHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (!_serviceInvokeHandlers.TryAdd(serviceName, handler))
            throw new DuplicateServiceNameException();

        return RegisterServiceCore(serviceName);
    }

    public ValueTask UnregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (!_serviceInvokeHandlers.TryRemove(serviceName, out _))
            return default;

        return UnregisterServiceCore(serviceName, cancellationToken);
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
    private readonly TaskCompletionSource _connectTaskSource = new();
    private AsyncLock _mutex = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, ServiceInvokeHandler> _serviceInvokeHandlers = new();
    private readonly ConcurrentDictionary<string, Subject<RouterMessage>> _subscriptions = new();
    private readonly MessageRouterOptions _options;

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
                await _connection.SendAsync(new ConnectRequest { AccessToken = _options.AccessToken });
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
            if (!_serviceInvokeHandlers.TryGetValue(message.ServiceName, out var handler))
                throw new UnknownServiceException();

            var response = await handler(message.ServiceName, message.Payload);
            await ConnectAsync();
            await _connection.SendAsync(new InvokeResponse(message.RequestId, response));
        }
        catch (Exception e)
        {
            await ConnectAsync();
            await _connection.SendAsync(new InvokeResponse(message.RequestId, payload: null, e.Message));
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

            case MessageType.Update:
                return HandleUpdateMessage((UpdateMessage)message);

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
        if (!_pendingRequests.TryRemove(message.ServiceName, out var tcs))
            return Task.CompletedTask;

        if (message.Error != null)
        {
            _serviceInvokeHandlers.TryRemove(message.ServiceName, out _);
            tcs.SetException(new MessageRouterException(message.Error));
        }
        else
        {
            tcs.SetResult(message);
        }

        return Task.CompletedTask;
    }

    private Task HandleUpdateMessage(UpdateMessage message)
    {
        if (!_subscriptions.TryGetValue(message.Topic, out var subject))
            return Task.CompletedTask;

        var routerMessage = new RouterMessage(message.Topic, message.Payload);
        subject.OnNext(routerMessage);

        return Task.CompletedTask;
    }

    private async Task ReadMessagesAsync()
    {
        while (_connectionState != ConnectionState.Closed)
        {
            var message = await _connection.ReceiveAsync();

            if (_connectionState == ConnectionState.Closed)
                break;

            await HandleMessage(message);
        }
    }

    private async ValueTask RegisterServiceCore(string serviceName)
    {
        await ConnectAsync();
        var tcs = _pendingRequests.GetOrAdd(serviceName, _ => new TaskCompletionSource<Message>());
        await _connection.SendAsync(new RegisterServiceRequest(serviceName));
        await tcs.Task;
    }

    private async ValueTask<IDisposable> SubscribeCore(
        string topicName,
        Subject<RouterMessage> topic,
        IObserver<RouterMessage> observer,
        CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);
        await _connection.SendAsync(new SubscribeMessage(topicName), cancellationToken);

        return topic.Subscribe(observer);
    }

    private async ValueTask UnregisterServiceCore(string serviceName, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);
        await _connection.SendAsync(new UnregisterServiceMessage(serviceName), cancellationToken);
    }

    private enum ConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Closed
    }

    private static class ThrowHelper
    {
        public static InvalidOperationException ConnectionClosed()
        {
            return new InvalidOperationException("The connection has been closed");
        }
    }
}
