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

using System.Diagnostics;
using System.Linq.Expressions;
using MorganStanley.ComposeUI.Messaging.Instrumentation;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;
using MorganStanley.ComposeUI.Messaging.TestUtils;

namespace MorganStanley.ComposeUI.Messaging.Server;

public class MessageRouterServerTests
{
    [Fact]
    public async Task It_responds_to_ConnectRequest_with_ConnectResponse()
    {
        var connectResponseReceived = new TaskCompletionSource<ConnectResponse>();
        var client = CreateClient();
        client.Handle<ConnectResponse>(connectResponseReceived.SetResult);

        await _server.ClientConnected(client.Object);
        await client.SendToServer(new ConnectRequest());
        var connectResponse = await connectResponseReceived.Task;

        connectResponse.ClientId.Should().NotBeNullOrEmpty();
        connectResponse.Error.Should().BeNull();
    }

    [Fact]
    public async Task It_accepts_connection_with_valid_token()
    {
        var connectResponseReceived = new TaskCompletionSource<ConnectResponse>();
        _accessTokenValidator
            .Setup(_ => _.Validate(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(default(ValueTask));
        var client = CreateClient();
        client.Handle<ConnectResponse>(connectResponseReceived.SetResult);

        await _server.ClientConnected(client.Object);
        await client.SendToServer(new ConnectRequest {AccessToken = "token"});
        var connectResponse = await connectResponseReceived.Task;

        connectResponse.ClientId.Should().NotBeNullOrEmpty();
        connectResponse.Error.Should().BeNull();
    }

    [Fact]
    public async Task It_accepts_connection_without_token_if_validator_is_not_registered()
    {
        var connectResponseReceived = new TaskCompletionSource<ConnectResponse>();
        var client = CreateClient();
        client.Handle<ConnectResponse>(connectResponseReceived.SetResult);

        var server = CreateServer(null);
        await server.ClientConnected(client.Object);
        await client.SendToServer(new ConnectRequest());
        var connectResponse = await connectResponseReceived.Task;

        connectResponse.Should().BeOfType<ConnectResponse>();
        connectResponse.ClientId.Should().NotBeNullOrEmpty();
        connectResponse.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-token")]
    public async Task It_rejects_connections_with_invalid_token(string? token)
    {
        var connectResponseReceived = new TaskCompletionSource<ConnectResponse>();

        _accessTokenValidator.Setup(_ => _.Validate(It.IsAny<string>(), It.IsAny<string?>()))
            .Throws(new InvalidOperationException("Invalid token"));

        var client = CreateClient();
        client.Handle<ConnectResponse>(connectResponseReceived.SetResult);

        await _server.ClientConnected(client.Object);
        await client.SendToServer(new ConnectRequest {AccessToken = token});
        var connectResponse = await connectResponseReceived.Task;

        connectResponse.Error!.Message.Should().Be("Invalid token");
    }


    [Fact]
    public async Task When_Publish_message_received_it_dispatches_the_message_to_the_subscribers()
    {
        var client1 = await CreateAndConnectClient();
        var client2 = await CreateAndConnectClient();

        await client1.SendToServer(RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));
        await client2.SendToServer(RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));

        await WaitForCompletionAsync();

        await client1.SendToServer(
            RegisterRequest(
                new PublishMessage
                {
                    Topic = "test-topic",
                    Payload = MessageBuffer.Create("test-payload"),
                    CorrelationId = "test-correlation-id"
                }));

        await WaitForCompletionAsync();

        Expression<Func<Protocol.Messages.TopicMessage, bool>> expectation =
            msg => msg.Topic == "test-topic"
                   && msg.Payload != null
                   && msg.Payload.GetString() == "test-payload"
                   && msg.CorrelationId == "test-correlation-id"
                   && msg.SourceId == client1.ClientId;

        client1.Expect(expectation);
        client2.Expect(expectation);
    }

    [Fact]
    public async Task It_does_not_dispatch_Topic_message_if_the_client_has_unsubscribed()
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(
            RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));

        await client.SendToServer(
            RegisterRequest(new PublishMessage {Topic = "test-topic"}));

        await WaitForCompletionAsync();

        client.Expect<Protocol.Messages.TopicMessage>(msg => msg.Topic == "test-topic", Times.Once);
        client.Invocations.Clear();

        await client.SendToServer(
            RegisterRequest(
                new UnsubscribeMessage {Topic = "test-topic"}));

        await client.SendToServer(
            RegisterRequest(
                new PublishMessage {Topic = "test-topic"}));

        await WaitForCompletionAsync();

        client.Expect<Protocol.Messages.TopicMessage>(msg => msg.Topic == "test-topic", Times.Never);
    }

    [Fact]
    public async Task Client_can_register_itself_as_a_service()
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(RegisterRequest(new RegisterServiceRequest {Endpoint = "test-service"}));
        await WaitForCompletionAsync();

        var registerServiceResponse = client.Received.OfType<RegisterServiceResponse>().First();
        registerServiceResponse.Error.Should().BeNull();
    }

    [Fact]
    public async Task Client_can_unregister_itself_as_a_service()
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(RegisterRequest(new RegisterServiceRequest {Endpoint = "test-service"}));
        await client.SendToServer(RegisterRequest(new UnregisterServiceRequest {Endpoint = "test-service"}));
        await WaitForCompletionAsync();

        var registerServiceResponse = client.Received.OfType<RegisterServiceResponse>().Single();
        registerServiceResponse.Error.Should().BeNull();
        var unregisterServiceResponse = client.Received.OfType<UnregisterServiceResponse>().Single();
        unregisterServiceResponse.Error.Should().BeNull();
    }

    [Fact]
    public async Task It_handles_service_invocation()
    {
        var service = await CreateAndConnectClient();

        service.Handle(
            (InvokeRequest req) => new InvokeResponse
                {RequestId = req.RequestId, Payload = MessageBuffer.Create("test-response")});

        var caller = await CreateAndConnectClient();

        await service.SendToServer(RegisterRequest(new RegisterServiceRequest {Endpoint = "test-service"}));
        await WaitForCompletionAsync();

        await caller.SendToServer(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service",
                    Payload = MessageBuffer.Create("test-payload")
                }));

        await WaitForCompletionAsync();

        service.Expect<InvokeRequest>(
            msg => msg.Endpoint == "test-service" && msg.Payload!.GetString() == "test-payload",
            Times.Once);

        caller.Expect<InvokeResponse>(
            msg => msg.RequestId == "1" && msg.Payload!.GetString() == "test-response" && msg.Error == null,
            Times.Once);
    }

    [Fact]
    public async Task It_handles_service_invocation_with_error()
    {
        var service = await CreateAndConnectClient();

        service.Handle(
            (InvokeRequest req) => new InvokeResponse
            {
                RequestId = req.RequestId,
                Error = new Error("Error", "Invoke failed")
            });

        var caller = await CreateAndConnectClient();

        await service.SendToServer(RegisterRequest(new RegisterServiceRequest {Endpoint = "test-service"}));
        await WaitForCompletionAsync();

        await caller.SendToServer(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service",
                    Payload = MessageBuffer.Create("test-payload")
                }));

        await WaitForCompletionAsync();

        service.Expect<InvokeRequest>(
            msg => msg.Endpoint == "test-service" && msg.Payload!.GetString() == "test-payload",
            Times.Once);

        caller.Expect<InvokeResponse>(
            msg => msg.RequestId == "1"
                   && msg.Error!.Name == "Error"
                   && msg.Error!.Message == "Invoke failed",
            Times.Once);
    }

    [Fact]
    public async Task It_fails_the_service_invocation_if_the_service_is_not_registered()
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service"
                }));

        await WaitForCompletionAsync();

        client.Expect<InvokeResponse>(
            msg => msg.RequestId == "1"
                   && msg.Error!.Name == MessageRouterErrors.UnknownEndpoint,
            Times.Once);
    }

    [Fact]
    public async Task It_fails_the_service_invocation_if_the_service_has_unregistered_itself()
    {
        var service = await CreateAndConnectClient();
        service.Handle<InvokeRequest, InvokeResponse>();
        var caller = await CreateAndConnectClient();

        await service.SendToServer(
            RegisterRequest(
                new RegisterServiceRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service"
                }));

        await WaitForCompletionAsync();

        await caller.SendToServer(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service"
                }));

        await WaitForCompletionAsync();

        caller.Expect<InvokeResponse>(
            msg => msg.RequestId == "1",
            Times.Once);
        caller.Invocations.Clear();

        await service.SendToServer(
            RegisterRequest(new UnregisterServiceRequest {RequestId = "2", Endpoint = "test-service"}));
        await WaitForCompletionAsync();

        await caller.SendToServer(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "2",
                    Endpoint = "test-service"
                }));

        await WaitForCompletionAsync();

        caller.Expect<InvokeResponse>(
            msg => msg.RequestId == "2" && msg.Error != null && msg.Error.Name == MessageRouterErrors.UnknownEndpoint,
            Times.Once);
    }

    [Fact]
    public async Task When_disposed_it_calls_CloseAsync_on_active_connections()
    {
        var connection = await CreateAndConnectClient();
        await connection.SendToServer(RegisterRequest(new PublishMessage {Topic = "test-topic"}));

        await WaitForCompletionAsync();

        await _server.DisposeAsync();

        connection.Verify(_ => _.CloseAsync(), Times.Once);
    }

    [Theory]
    [InlineData("Subscribe")]
    [InlineData("Unsubscribe")]
    [InlineData("Publish")]
    [InlineData("Invoke")]
    [InlineData("InvokeResponse")]
    [InlineData("RegisterService")]
    [InlineData("UnregisterService")]
    public async Task Messages_sent_before_Connect_are_not_processed_and_the_connection_is_closed(string messageKind)
    {
        var subscriber = await CreateAndConnectClient();
        await subscriber.SendToServer(RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));
        await WaitForCompletionAsync();
        var client = CreateClient();
        await _server.ClientConnected(client.Object);

        await client.SendToServer(CreateMessage(messageKind));
        await WaitForCloseAsync(client);

        client.Received.Should().BeEmpty("the server must not answer a client that has not connected");
        subscriber.Received.OfType<Protocol.Messages.TopicMessage>().Should().BeEmpty();
    }

    [Fact]
    public async Task A_service_registered_before_Connect_does_not_block_the_endpoint()
    {
        var attacker = CreateClient();
        await _server.ClientConnected(attacker.Object);
        await attacker.SendToServer(new RegisterServiceRequest {RequestId = "1", Endpoint = "test-service"});
        await WaitForCloseAsync(attacker);
        var service = await CreateAndConnectClient();
        var registered = new TaskCompletionSource<RegisterServiceResponse>();
        service.Handle<RegisterServiceResponse>(registered.SetResult);

        await service.SendToServer(new RegisterServiceRequest {RequestId = "2", Endpoint = "test-service"});
        var response = await registered.Task.WaitAsync(TestTimeout);

        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task It_closes_the_connection_after_a_rejected_Connect_and_ignores_the_messages_behind_it()
    {
        _accessTokenValidator
            .Setup(_ => _.Validate(It.IsAny<string>(), "invalid-token"))
            .Throws(new InvalidOperationException("Invalid token"));
        var subscriber = await CreateAndConnectClient();
        await subscriber.SendToServer(RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));
        await WaitForCompletionAsync();
        var client = CreateClient();
        await _server.ClientConnected(client.Object);

        await client.SendToServer(new ConnectRequest {AccessToken = "invalid-token"});
        await client.SendToServer(new PublishMessage {Topic = "test-topic", Payload = MessageBuffer.Create("x")});
        await WaitForCloseAsync(client);

        client.Expect<ConnectResponse>(msg => msg.Error != null && msg.Error.Message == "Invalid token", Times.Once);
        subscriber.Received.OfType<Protocol.Messages.TopicMessage>().Should().BeEmpty();
    }

    [Fact]
    public async Task A_second_Connect_closes_the_connection()
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(new ConnectRequest());

        await WaitForCloseAsync(client);
    }

    [Theory]
    [InlineData("ConnectResponse")]
    [InlineData("SubscribeResponse")]
    [InlineData("UnsubscribeResponse")]
    [InlineData("PublishResponse")]
    [InlineData("RegisterServiceResponse")]
    [InlineData("UnregisterServiceResponse")]
    [InlineData("Topic")]
    public async Task Message_types_that_only_the_server_may_send_close_the_connection(string messageKind)
    {
        var client = await CreateAndConnectClient();

        await client.SendToServer(CreateMessage(messageKind));

        await WaitForCloseAsync(client);
    }

    [Fact]
    public async Task The_gate_applies_when_no_access_token_validator_is_registered()
    {
        var server = CreateServer(null);
        var client = CreateClient();
        await server.ClientConnected(client.Object);

        await client.SendToServer(new PublishMessage {Topic = "test-topic"});

        await WaitForCloseAsync(client);
    }

    [Fact]
    public async Task A_Publish_sent_right_behind_Connect_without_waiting_for_the_response_is_delivered()
    {
        var subscriber = await CreateAndConnectClient();
        await subscriber.SendToServer(RegisterRequest(new SubscribeMessage {Topic = "test-topic"}));
        await WaitForCompletionAsync();
        var publisher = CreateClient();
        await _server.ClientConnected(publisher.Object);

        await publisher.SendToServer(new ConnectRequest());
        await publisher.SendToServer(
            new PublishMessage {Topic = "test-topic", Payload = MessageBuffer.Create("test-payload")});
        await WaitUntilAsync(() => subscriber.Received.OfType<Protocol.Messages.TopicMessage>().Any());

        WasClosed(publisher).Should().BeFalse();
    }

    [Fact]
    public async Task A_connection_that_never_sends_Connect_is_closed_after_the_connect_timeout()
    {
        var server = CreateServer(null, TimeSpan.FromMilliseconds(100));
        var client = CreateClient();

        await server.ClientConnected(client.Object);

        await WaitForCloseAsync(client);
    }

    [Fact]
    public async Task The_connect_timeout_does_not_close_a_client_that_has_connected()
    {
        var server = CreateServer(null, TimeSpan.FromMilliseconds(100));
        var client = CreateClient();
        await server.ClientConnected(client.Object);
        await client.Connect();

        await Task.Delay(TimeSpan.FromMilliseconds(500));

        WasClosed(client).Should().BeFalse();
    }

    public MessageRouterServerTests()
    {
        _server = CreateServer(_accessTokenValidator.Object);
        _diagnosticObserver = new MessageRouterDiagnosticObserver(_server);
    }

    private MessageRouterServer _server;
    private Mock<IAccessTokenValidator> _accessTokenValidator = new();
    private MessageRouterDiagnosticObserver _diagnosticObserver;

    private MessageRouterServer CreateServer(IAccessTokenValidator? accessTokenValidator, TimeSpan? connectTimeout = null) =>
        new MessageRouterServer(new MessageRouterServerDependencies(accessTokenValidator, connectTimeout));

    private MockClientConnection CreateClient() => new();

    private async Task<MockClientConnection> CreateAndConnectClient()
    {
        var client = CreateClient();
        await _server.ClientConnected(client.Object);
        await client.Connect();

        return client;
    }

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(2);

    private static Message CreateMessage(string kind) =>
        kind switch
        {
            "Subscribe" => new SubscribeMessage {RequestId = "1", Topic = "test-topic"},
            "Unsubscribe" => new UnsubscribeMessage {RequestId = "1", Topic = "test-topic"},
            "Publish" => new PublishMessage {Topic = "test-topic", Payload = MessageBuffer.Create("test-payload")},
            "Invoke" => new InvokeRequest {RequestId = "1", Endpoint = "test-service"},
            "InvokeResponse" => new InvokeResponse {RequestId = "1"},
            "RegisterService" => new RegisterServiceRequest {RequestId = "1", Endpoint = "test-service"},
            "UnregisterService" => new UnregisterServiceRequest {RequestId = "1", Endpoint = "test-service"},
            "ConnectResponse" => new ConnectResponse {ClientId = "client"},
            "SubscribeResponse" => new SubscribeResponse {RequestId = "1"},
            "UnsubscribeResponse" => new UnsubscribeResponse {RequestId = "1"},
            "PublishResponse" => new PublishResponse {RequestId = "1"},
            "RegisterServiceResponse" => new RegisterServiceResponse {RequestId = "1"},
            "UnregisterServiceResponse" => new UnregisterServiceResponse {RequestId = "1"},
            "Topic" => new Protocol.Messages.TopicMessage {Topic = "test-topic", SourceId = "client"},
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static bool WasClosed(MockClientConnection client) =>
        client.Invocations.Any(invocation => invocation.Method.Name == nameof(IClientConnection.CloseAsync));

    private static Task WaitForCloseAsync(MockClientConnection client) =>
        WaitUntilAsync(() => WasClosed(client), "the server to close the connection");

    private static async Task WaitUntilAsync(Func<bool> condition, string description = "the condition")
    {
        var deadline = DateTime.UtcNow + TestTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}.");
            }

            await Task.Delay(10);
        }
    }

    private TMessage RegisterRequest<TMessage>(TMessage message) where TMessage : Message
    {
        return _diagnosticObserver.RegisterRequest(message);
    }

    private async Task WaitForCompletionAsync()
    {
        await _diagnosticObserver.WaitForCompletionAsync(TestTimeout);
    }
}