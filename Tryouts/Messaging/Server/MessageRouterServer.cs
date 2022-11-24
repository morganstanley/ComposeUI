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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Exceptions;
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Messages;
using MorganStanley.ComposeUI.Tryouts.Messaging.Server.Transport.Abstractions;

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server;

// TODO: Also implement IMessageRouter to speed up in-process messaging
internal class MessageRouterServer : IMessageRouterServer
{
    public MessageRouterServer(ILogger<MessageRouterServer>? logger)
    {
        _logger = logger ?? NullLogger<MessageRouterServer>.Instance;
    }

    public async ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();

        foreach (var client in _clients.Values)
        {
            await client.Connection.DisposeAsync();
        }
    }

    public ValueTask ClientConnected(ISubscriber subscriber)
    {
        var client = new Client(subscriber);
        _connectionToClient[subscriber] = client;
        ProcessMessagesAsync(client, _stopTokenSource.Token);

        return default;
    }

    public ValueTask ClientDisconnected(ISubscriber subscriber)
    {
        if (!_connectionToClient.TryRemove(subscriber, out var client))
            return default;

        _clients.TryRemove(client.Id, out _);

        return default;
    }

    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private readonly ILogger<MessageRouterServer> _logger;
    private readonly ConcurrentDictionary<string, ServiceInvocation> _serviceInvocations = new();
    private readonly ConcurrentDictionary<string, Guid> _serviceRegistrations = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly ConcurrentDictionary<string, Topic> _topics = new();
    private readonly ConcurrentDictionary<ISubscriber, Client> _connectionToClient = new();

    private async Task HandleConnectRequest(
        Client client,
        ConnectRequest message,
        CancellationToken cancellationToken)
    {
        client.Id = Guid.NewGuid();

        if (!_clients.TryAdd(client.Id, client))
            return;

        await client.Connection.SendAsync(new ConnectResponse(client.Id), cancellationToken);
    }

    private async Task HandleInvokeRequest(
        Client client,
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

            await serviceClient.Connection.SendAsync(
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

            await client.Connection.SendAsync(
                new InvokeResponse(
                    message.RequestId,
                    payload: null,
                    e.Message),
                CancellationToken.None);
        }
    }

    private async Task HandleInvokeResponse(
        Client client,
        InvokeResponse message,
        CancellationToken cancellationToken)
    {
        if (!_serviceInvocations.TryGetValue(message.RequestId, out var request))
            return;

        if (!_clients.TryGetValue(request.CallerClientId, out var caller))
            return;

        var response = new InvokeResponse(request.CallerRequestId, message.Payload, message.Error);
        await caller.Connection.SendAsync(response, CancellationToken.None);
        _serviceInvocations.TryRemove(request.ServiceRequestId, out _);
    }

    private async Task HandlePublishMessage(
        Client client,
        PublishMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic))
            return;

        var topic = _topics.GetOrAdd(message.Topic, topicName => new Topic(topicName, ImmutableHashSet<Guid>.Empty));
        var outgoingMessage = new UpdateMessage(message.Topic, message.Payload);

        //var outgoingMessage = new UpdateMessage(message.Topic, Encoding.UTF8.GetBytes(message.Payload));
        await Task.WhenAll(
            topic.Subscribers.Select(
                async subscriberId =>
                {
                    if (_clients.TryGetValue(subscriberId, out var subscriber))
                        await subscriber.Connection.SendAsync(outgoingMessage, cancellationToken);
                }));
    }

    private async Task HandleRegisterServiceRequest(
        Client client,
        RegisterServiceRequest message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ServiceName))
            return;

        try
        {
            if (!_serviceRegistrations.TryAdd(message.ServiceName, client.Id))
                throw new DuplicateServiceNameException();

            await client.Connection.SendAsync(new RegisterServiceResponse(message.ServiceName));
        }
        catch (Exception e)
        {
            await client.Connection.SendAsync(new RegisterServiceResponse(message.ServiceName, e.Message));
        }
    }

    private async Task HandleSubscribeMessage(
        Client client,
        SubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic))
            return;

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
        Client client,
        UnregisterServiceMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ServiceName))
            return;

        _serviceRegistrations.TryRemove(new KeyValuePair<string, Guid>(message.ServiceName, client.Id));
    }

    private async Task HandleUnsubscribeMessage(
        Client client,
        UnsubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic))
            return;

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

    private async void ProcessMessagesAsync(Client client, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in client.Connection.ReceiveAsync(cancellationToken))
            {
                try
                {
                    switch (message.Type)
                    {
                        case MessageType.Connect:
                            await HandleConnectRequest(client, (ConnectRequest)message, cancellationToken);

                            break;

                        case MessageType.Subscribe:
                            await HandleSubscribeMessage(client, (SubscribeMessage)message, cancellationToken);

                            break;

                        case MessageType.Unsubscribe:
                            await HandleUnsubscribeMessage(client, (UnsubscribeMessage)message, cancellationToken);

                            break;

                        case MessageType.Publish:
                            await HandlePublishMessage(client, (PublishMessage)message, cancellationToken);

                            break;

                        case MessageType.Invoke:
                            await HandleInvokeRequest(client, (InvokeRequest)message, cancellationToken);

                            break;

                        case MessageType.InvokeResponse:
                            await HandleInvokeResponse(client, (InvokeResponse)message, cancellationToken);

                            break;

                        case MessageType.RegisterService:
                            await HandleRegisterServiceRequest(
                                client,
                                (RegisterServiceRequest)message,
                                cancellationToken);

                            break;

                        case MessageType.UnregisterService:
                            await HandleUnregisterServiceMessage(
                                client,
                                (UnregisterServiceMessage)message,
                                cancellationToken);

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Exception thrown while processing a message from client '{ClientId}': {ExceptionMessage}",
                        client.Id,
                        e.Message);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Unhandled exception while processing messages from client '{ClientId}': {ExceptionMessage}",
                client.Id,
                e.Message);
        }
    }

    private class Client
    {
        public Client(ISubscriber connection)
        {
            Connection = connection;
        }

        public ISubscriber Connection { get; }
        public Guid Id { get; set; } = Guid.Empty;
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
