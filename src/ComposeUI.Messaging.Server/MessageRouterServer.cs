// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading.Channels;
using ComposeUI.Messaging.Core.Exceptions;
using ComposeUI.Messaging.Core.Messages;
using ComposeUI.Messaging.Core.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace ComposeUI.Messaging.Prototypes;

public class MessageRouterServer : IAsyncDisposable
{
    public MessageRouterServer(ILogger<MessageRouterServer>? logger)
    {
        _logger = logger ?? NullLogger<MessageRouterServer>.Instance;
    }

    public async Task HandleWebSocketRequest(WebSocket webSocket, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("WebSocket client connected");
        await using var client = new ClientConnection();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token);
        try
        {
            await Task.WhenAll(
                ReceiveMessagesAsync(client, webSocket, cts.Token),
                ProcessMessagesAsync(client, cts.Token),
                SendMessagesAsync(client, webSocket, cts.Token));
        }
        finally
        {
            _clients.TryRemove(client.Id, out _);
            if (_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("WebSocket client disconnected");
        }
    }

    public ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();
        return default;
    }

    private readonly ConcurrentDictionary<Guid, ClientConnection> _clients = new();
    private readonly ILogger<MessageRouterServer> _logger;
    private readonly ConcurrentDictionary<string, ServiceInvocation> _serviceInvocations = new();
    private readonly ConcurrentDictionary<string, Guid> _serviceRegistrations = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly ConcurrentDictionary<string, Topic> _topics = new();

    private async Task HandleConnectRequest(
        ClientConnection client,
        ConnectRequest message,
        CancellationToken cancellationToken)
    {
        if (client.Id != Guid.Empty)
        {
            if (client.Id != message.ClientId)
            {
                // TODO: Kick out client for sending an invalid connect message?
            }

            return;
        }

        client.Id = message.ClientId ?? Guid.NewGuid();
        if (!_clients.TryAdd(client.Id, client)) return;
        await client.OutputChannel.Writer.WriteAsync(new ConnectResponse(client.Id), cancellationToken);
    }

    private async Task HandleInvokeRequest(
        ClientConnection client,
        InvokeRequest message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_serviceRegistrations.TryGetValue(message.ServiceName, out var serviceId))
                throw new UnknownServiceException();
            if (!_clients.TryGetValue(serviceId, out var serviceClient))
                throw new ServiceUnavailableException();
            var request = new ServiceInvocation(message.RequestId, Guid.NewGuid().ToString(), client.Id, serviceId);
            if (!_serviceInvocations.TryAdd(request.ServiceRequestId, request))
                throw new DuplicateRequestIdException();
            await serviceClient.OutputChannel.Writer.WriteAsync(
                new InvokeRequest(request.ServiceRequestId, message.ServiceName, message.Payload),
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                message:
                "An exception of type {ExceptionClassName} was thrown while processing an invocation for service '{ServiceName}'.",
                e.GetType().FullName,
                message.ServiceName);
            await client.OutputChannel.Writer.WriteAsync(
                new InvokeResponse(message.RequestId, payload: null, e.Message));
        }
    }

    private async Task HandleInvokeResponse(
        ClientConnection client,
        InvokeResponse message,
        CancellationToken cancellationToken)
    {
        if (!_serviceInvocations.TryGetValue(message.RequestId, out var request)) return;
        if (!_clients.TryGetValue(request.CallerClientId, out var caller)) return;
        var response = new InvokeResponse(request.CallerRequestId, message.Payload, message.Error);
        await caller.OutputChannel.Writer.WriteAsync(response, CancellationToken.None);
        _serviceInvocations.TryRemove(request.ServiceRequestId, out _);
    }

    private async Task HandlePublishMessage(
        ClientConnection client,
        PublishMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic)) return;
        var topic = _topics.GetOrAdd(message.Topic, topicName => new Topic(topicName, ImmutableHashSet<Guid>.Empty));
        var outgoingMessage = new UpdateMessage(message.Topic, message.Payload);
        //var outgoingMessage = new UpdateMessage(message.Topic, Encoding.UTF8.GetBytes(message.Payload));
        await Task.WhenAll(
            topic.Subscribers.Select(
                async subscriberId =>
                {
                    if (_clients.TryGetValue(subscriberId, out var subscriber))
                        await subscriber.OutputChannel.Writer.WriteAsync(outgoingMessage, cancellationToken);
                }));
    }

    private async Task HandleRegisterServiceRequest(
        ClientConnection client,
        RegisterServiceRequest message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ServiceName)) return;
        try
        {
            if (!_serviceRegistrations.TryAdd(message.ServiceName, client.Id))
                throw new DuplicateServiceNameException();
            await client.OutputChannel.Writer.WriteAsync(new RegisterServiceResponse(message.ServiceName));
        }
        catch (Exception e)
        {
            await client.OutputChannel.Writer.WriteAsync(new RegisterServiceResponse(message.ServiceName, e.Message));
        }
    }

    private async Task HandleSubscribeMessage(
        ClientConnection client,
        SubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic)) return;
        var topic = _topics.AddOrUpdate(
            message.Topic,
            (topicName, client) => new Topic(topicName, ImmutableHashSet<Guid>.Empty.Add(client.Id)),
            (topicName, topic, client) =>
            {
                topic.Subscribers = topic.Subscribers.Add(client.Id);
                return topic;
            },
            client);
    }

    private async Task HandleUnregisterServiceMessage(
        ClientConnection client,
        UnregisterServiceMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ServiceName)) return;
        _serviceRegistrations.TryRemove(new KeyValuePair<string, Guid>(message.ServiceName, client.Id));
    }

    private async Task HandleUnsubscribeMessage(
        ClientConnection client,
        UnsubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic)) return;
        var topic = _topics.AddOrUpdate(
            message.Topic,
            (topicName, client) => new Topic(topicName, ImmutableHashSet<Guid>.Empty.Add(client.Id)),
            (topicName, topic, client) =>
            {
                topic.Subscribers = topic.Subscribers.Remove(client.Id);
                return topic;
            },
            client);
    }

    private async Task ProcessMessagesAsync(ClientConnection client, CancellationToken cancellationToken)
    {
        await foreach (var message in client.InputChannel.Reader.ReadAllAsync(cancellationToken))
            switch (message.Type)
            {
                case MessageType.Connect:
                    await HandleConnectRequest(client, (ConnectRequest) message, cancellationToken);
                    break;
                case MessageType.Subscribe:
                    await HandleSubscribeMessage(client, (SubscribeMessage) message, cancellationToken);
                    break;
                case MessageType.Unsubscribe:
                    await HandleUnsubscribeMessage(client, (UnsubscribeMessage) message, cancellationToken);
                    break;
                case MessageType.Publish:
                    await HandlePublishMessage(client, (PublishMessage) message, cancellationToken);
                    break;
                case MessageType.Invoke:
                    await HandleInvokeRequest(client, (InvokeRequest) message, cancellationToken);
                    break;
                case MessageType.InvokeResponse:
                    await HandleInvokeResponse(client, (InvokeResponse) message, cancellationToken);
                    break;
                case MessageType.RegisterService:
                    await HandleRegisterServiceRequest(client, (RegisterServiceRequest) message, cancellationToken);
                    break;
                case MessageType.UnregisterService:
                    await HandleUnregisterServiceMessage(client, (UnregisterServiceMessage) message, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    }

    private async Task ReceiveMessagesAsync(
        ClientConnection client,
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        var pipe = new Pipe();

        try
        {
            while (!webSocket.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested)
            {
                var buffer = pipe.Writer.GetMemory(1024 * 4);
                var receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                pipe.Writer.Advance(receiveResult.Count);
                if (receiveResult.EndOfMessage)
                {
                    await pipe.Writer.FlushAsync(CancellationToken.None);
                    var readResult = await pipe.Reader.ReadAsync(CancellationToken.None);
                    var readBuffer = readResult.Buffer;
                    while (!readBuffer.IsEmpty && TryReadMessage(ref readBuffer, out var message))
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug($"Received message '{message.Type}' from client '{client.Id}'");
                        await client.InputChannel.Writer.WriteAsync(message, cancellationToken);
                    }

                    pipe.Reader.AdvanceTo(readBuffer.Start, readBuffer.End);
                }
            }
        }
        catch (Exception e)
        {
            client.InputChannel.Writer.TryComplete(e);
        }
        finally
        {
            client.InputChannel.Writer.TryComplete();
            client.OutputChannel.Writer.TryComplete();
        }
    }

    private async Task SendMessagesAsync(
        ClientConnection client,
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        await foreach (var message in client.OutputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if (webSocket.State != WebSocketState.Open) break;
            var buffer = JsonMessageSerializer.SerializeMessage(message);
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Sending message '{message.Type}' to client '{client.Id}'");
            await webSocket.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                WebSocketMessageFlags.EndOfMessage,
                cancellationToken);
        }
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out Message? message)
    {
        var innerBuffer = buffer;
        try
        {
            message = JsonMessageSerializer.DeserializeMessage(ref innerBuffer);
            buffer = buffer.Slice(innerBuffer.Start);
            return true;
        }
        catch
        {
            message = null;
            return false;
        }
    }

    public class ClientConnection : IAsyncDisposable
    {
        public ClientConnection()
        {
        }

        public Guid Id { get; set; } = Guid.Empty;
        public Channel<Message> InputChannel { get; } = Channel.CreateUnbounded<Message>();
        public Channel<Message> OutputChannel { get; } = Channel.CreateUnbounded<Message>();

        public async ValueTask DisposeAsync()
        {
            InputChannel.Writer.TryComplete();
            OutputChannel.Writer.TryComplete();
            await Task.WhenAll(InputChannel.Reader.Completion, OutputChannel.Reader.Completion);
        }
    }

    private class Topic
    {
        public Topic(string name, ImmutableHashSet<Guid> subscribers)
        {
            Name = name;
            Subscribers = subscribers;
        }

        public string Name { get; }

        public ImmutableHashSet<Guid> Subscribers { get; set; } = ImmutableHashSet<Guid>.Empty;
    }

    private class ServiceInvocation
    {
        public ServiceInvocation(
            string callerRequestId,
            string serviceRequestId,
            Guid callerClientId,
            Guid serviceClientId)
        {
            CallerRequestId = callerRequestId;
            CallerClientId = callerClientId;
            ServiceClientId = serviceClientId;
            ServiceRequestId = serviceRequestId;
        }

        public string CallerRequestId { get; }
        public string ServiceRequestId { get; }
        public Guid CallerClientId { get; }
        public Guid ServiceClientId { get; }
    }
}