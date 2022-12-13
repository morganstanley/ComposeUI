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
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Server;

// TODO: Also implement IMessageRouter to speed up in-process messaging
internal class MessageRouterServer : IMessageRouterServer
{
    public MessageRouterServer(
        MessageRouterServerDependencies dependencies,
        ILogger<MessageRouterServer>? logger = null)
    {
        _accessTokenValidator = dependencies.AccessTokenValidator;
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

    public ValueTask ClientConnected(IClientConnection connection)
    {
        var client = new Client(connection);
        _connectionToClient[connection] = client;
        ProcessMessagesAsync(client, _stopTokenSource.Token);

        return default;
    }

    public ValueTask ClientDisconnected(IClientConnection connection)
    {
        if (!_connectionToClient.TryRemove(connection, out var client)
            || !_clients.TryRemove(client.ClientId, out _))
        {
            return default;
        }

        client.StopTokenSource.Cancel();
        // TODO: Clean up leftover junk like topic subscriptions
        return new ValueTask(client.StopTaskSource.Task);
    }

    private readonly ConcurrentDictionary<string, Client> _clients = new();
    private readonly ILogger<MessageRouterServer> _logger;
    private readonly ConcurrentDictionary<string, ServiceInvocation> _serviceInvocations = new();
    private readonly ConcurrentDictionary<string, string> _serviceRegistrations = new();
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly ConcurrentDictionary<string, Topic> _topics = new();
    private readonly ConcurrentDictionary<IClientConnection, Client> _connectionToClient = new();
    private readonly IAccessTokenValidator? _accessTokenValidator;

    private async Task HandleConnectRequest(
        Client client,
        ConnectRequest message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_accessTokenValidator != null)
            {
                await _accessTokenValidator.Validate(client.ClientId, message.AccessToken);
            }

            await client.Connection.SendAsync(
                new ConnectResponse { ClientId = client.ClientId },
                CancellationToken.None);

            _clients.TryAdd(client.ClientId, client);
        }
        catch (Exception e)
        {
            await client.Connection.SendAsync(new ConnectResponse { Error = e.Message }, CancellationToken.None);
        }
    }

    private async Task HandleInvokeRequest(
        Client client,
        InvokeRequest message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!_serviceRegistrations.TryGetValue(message.ServiceName, out var serviceClientId)
                || !_clients.TryGetValue(serviceClientId, out var serviceClient))
            {
                throw new UnknownServiceException(message.ServiceName);
            }

            var request = new ServiceInvocation(
                message.RequestId,
                Guid.NewGuid().ToString(),
                client.ClientId,
                serviceClientId);

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
                "An exception of type {ExceptionClassName} was thrown while processing an invocation for service '{ServiceName}'.",
                e.GetType().FullName,
                message.ServiceName);

            await client.Connection.SendAsync(
                new InvokeResponse(
                    message.RequestId,
                    null,
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

        var topic = _topics.GetOrAdd(message.Topic, topicName => new Topic(topicName, ImmutableHashSet<string>.Empty));
        var outgoingMessage = new UpdateMessage(message.Topic, message.Payload, message.Scope);

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
            if (!_serviceRegistrations.TryAdd(message.ServiceName, client.ClientId))
                throw new DuplicateServiceNameException(message.ServiceName);

            await client.Connection.SendAsync(new RegisterServiceResponse(message.ServiceName), CancellationToken.None);
        }
        catch (Exception e)
        {
            await client.Connection.SendAsync(
                new RegisterServiceResponse(message.ServiceName, e.Message),
                CancellationToken.None);
        }
    }

    private Task HandleSubscribeMessage(
        Client client,
        SubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic))
            return Task.CompletedTask;

        var topic = _topics.AddOrUpdate(
            message.Topic,
            // ReSharper disable once VariableHidesOuterVariable
            static (topicName, client) => new Topic(topicName, ImmutableHashSet<string>.Empty.Add(client.ClientId)),
            // ReSharper disable once VariableHidesOuterVariable
            static (topicName, topic, client) =>
            {
                topic.Subscribers = topic.Subscribers.Add(client.ClientId);

                return topic;
            },
            client);

        return Task.CompletedTask;
    }

    private Task HandleUnregisterServiceMessage(
        Client client,
        UnregisterServiceMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ServiceName))
            return Task.CompletedTask;

        _serviceRegistrations.TryRemove(new KeyValuePair<string, string>(message.ServiceName, client.ClientId));

        return Task.CompletedTask;
    }

    private Task HandleUnsubscribeMessage(
        Client client,
        UnsubscribeMessage message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Topic))
            return Task.CompletedTask;

        var topic = _topics.AddOrUpdate(
            message.Topic,
            // ReSharper disable once VariableHidesOuterVariable
            static (topicName, client) => new Topic(topicName, ImmutableHashSet<string>.Empty),
            // ReSharper disable once VariableHidesOuterVariable
            static (topicName, topic, client) =>
            {
                topic.Subscribers = topic.Subscribers.Remove(client.ClientId);

                return topic;
            },
            client);

        return Task.CompletedTask;
    }

    private async void ProcessMessagesAsync(Client client, CancellationToken cancellationToken)
    {
        try
        {
            while (!client.StopTokenSource.IsCancellationRequested)
            {
                var message = await client.Connection.ReceiveAsync(cancellationToken);

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
                        client.ClientId,
                        e.Message);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Unhandled exception while processing messages from client '{ClientId}': {ExceptionMessage}",
                client.ClientId,
                e.Message);

            client.StopTokenSource.Cancel();
        }

        client.StopTaskSource.TrySetResult();
    }

    private class Client
    {
        public Client(IClientConnection connection)
        {
            Connection = connection;
        }

        public IClientConnection Connection { get; }
        public CancellationTokenSource StopTokenSource { get; } = new();
        public TaskCompletionSource StopTaskSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string ClientId { get; } = Guid.NewGuid().ToString("N");
    }

    private class Topic
    {
        public Topic(string name, ImmutableHashSet<string> subscribers)
        {
            Name = name;
            Subscribers = subscribers;
        }

        public string Name { get; }

        public ImmutableHashSet<string> Subscribers { get; set; } = ImmutableHashSet<string>.Empty;
    }

    private class ServiceInvocation
    {
        public ServiceInvocation(
            string callerRequestId,
            string serviceRequestId,
            string callerClientId,
            string serviceClientId)
        {
            CallerRequestId = callerRequestId;
            CallerClientId = callerClientId;
            ServiceClientId = serviceClientId;
            ServiceRequestId = serviceRequestId;
        }

        public string CallerRequestId { get; }
        public string ServiceRequestId { get; }
        public string CallerClientId { get; }
        public string ServiceClientId { get; }
    }
}

