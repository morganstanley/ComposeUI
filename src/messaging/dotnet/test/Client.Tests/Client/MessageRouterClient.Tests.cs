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

using System.Linq.Expressions;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.TestUtils;
using TaskExtensions = MorganStanley.ComposeUI.Testing.TaskExtensions;

namespace MorganStanley.ComposeUI.Messaging.Client;

public class MessageRouterClientTests : IAsyncLifetime
{
    [Fact]
    public async Task DisposeAsync_does_not_invoke_the_connection_when_called_before_connecting()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        _connectionMock.Verify(_ => _.DisposeAsync());
        _connectionMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_connection()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        await messageRouter.DisposeAsync();

        _connectionMock.Verify(_ => _.DisposeAsync());
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_if_the_client_was_already_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();
        await messageRouter.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_connection_exactly_once()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();
        await messageRouter.DisposeAsync();

        _connectionMock.Verify(_ => _.DisposeAsync(), Times.Once);
    }

    [Fact (Skip ="Ci fail")]
    public async Task DisposeAsync_calls_OnError_on_active_subscribers()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var subscriber = new Mock<IAsyncObserver<TopicMessage>>();
        await messageRouter.SubscribeAsync("test-topic", subscriber.Object);

        await messageRouter.DisposeAsync();

        subscriber.Verify(
            _ => _.OnErrorAsync(It.Is<MessageRouterException>(e => e.Name == MessageRouterErrors.ConnectionClosed)));
        subscriber.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisposeAsync_completes_pending_requests_with_a_MessageRouterException()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var invokeTask = messageRouter.InvokeAsync("test-endpoint");
        await messageRouter.DisposeAsync();

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask);
    }

    [Fact]
    public async Task ConnectAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(async () => await messageRouter.ConnectAsync());
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task ConnectAsync_sends_a_ConnectRequest_and_waits_for_a_ConnectResponse()
    {
        var connectRequestReceived = new TaskCompletionSource();
        _connectionMock.Handle<ConnectRequest>(_ => connectRequestReceived.SetResult());
        await using var messageRouter = CreateMessageRouter();

        var connectTask = messageRouter.ConnectAsync();
        await connectRequestReceived.Task;

        connectTask.IsCompleted.Should().BeFalse();

        await _connectionMock.SendToClient(new ConnectResponse {ClientId = "client-id"});
        await connectTask;

        messageRouter.ClientId.Should().Be("client-id");
    }

    [Fact]
    public async Task ConnectAsync_throws_a_MessageRouterException_if_the_ConnectResponse_contains_an_error()
    {
        var connectRequestReceived = new TaskCompletionSource();
        _connectionMock.Handle<ConnectRequest>(_ => connectRequestReceived.SetResult());
        await using var messageRouter = CreateMessageRouter();

        var connectTask = messageRouter.ConnectAsync();
        await connectRequestReceived.Task;

        connectTask.IsCompleted.Should().BeFalse();

        await _connectionMock.SendToClient(new ConnectResponse {Error = new Error("Error", "Fail")});

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await connectTask);
        exception.Name.Should().Be("Error");
        exception.Message.Should().Be("Fail");
    }

    [Fact]
    public async Task PublishAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await messageRouter.PublishAsync("test-topic"));
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task PublishAsync_sends_a_PublishMessage()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        await messageRouter.PublishAsync(
            "test-topic",
            "test-payload",
            new PublishOptions
                {CorrelationId = "test-correlation-id", Scope = MessageScope.FromClientId("other-client")});

        _connectionMock.Expect<PublishMessage>(
            msg => msg.Topic == "test-topic"
                   && msg.Payload != null
                   && msg.Payload.GetString() == "test-payload"
                   && msg.CorrelationId == "test-correlation-id"
                   && msg.Scope == MessageScope.FromClientId("other-client"));
    }

    [Fact]
    public async Task SubscribeAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await messageRouter.SubscribeAsync(
                    "test-topic",
                    new Mock<IAsyncObserver<TopicMessage>>().Object));
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task SubscribeAsync_sends_a_Subscribe_message()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        await messageRouter.SubscribeAsync("test-topic", new Mock<IAsyncObserver<TopicMessage>>().Object);

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic");
    }

    [Fact]
    public async Task SubscribeAsync_only_sends_a_Subscribe_message_on_the_first_subscription()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        await messageRouter.SubscribeAsync("test-topic", new Mock<IAsyncObserver<TopicMessage>>().Object);
        await messageRouter.SubscribeAsync("test-topic", new Mock<IAsyncObserver<TopicMessage>>().Object);
        await messageRouter.SubscribeAsync("test-topic", new Mock<IAsyncObserver<TopicMessage>>().Object);

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
    }

    [Fact]
    public async Task When_Topic_message_received_it_invokes_the_subscribers()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        var sub1 = new Mock<IAsyncObserver<TopicMessage>>();
        var sub2 = new Mock<IAsyncObserver<TopicMessage>>();
        await messageRouter.SubscribeAsync("test-topic", sub1.Object);
        await messageRouter.SubscribeAsync("test-topic", sub2.Object);

        await _connectionMock.SendToClient(
            new Protocol.Messages.TopicMessage
            {
                Topic = "test-topic",
                Payload = MessageBuffer.Create("test-payload"),
                SourceId = "other-client",
                CorrelationId = "test-correlation-id",
            });

        await TaskExtensions.WaitForBackgroundTasksAsync();

        Expression<Func<IAsyncObserver<TopicMessage>, ValueTask>> expectedInvocation =
            _ => _.OnNextAsync(
                It.Is<TopicMessage>(
                    msg => msg.Topic == "test-topic"
                           && msg.Payload != null
                           && msg.Payload.GetString() == "test-payload"
                           && msg.Context.SourceId == "other-client"
                           && msg.Context.CorrelationId == "test-correlation-id"));

        sub1.Verify(expectedInvocation, Times.Once);
        sub2.Verify(expectedInvocation, Times.Once);
    }

    [Fact]
    public async Task When_Topic_message_received_it_keeps_processing_messages_if_the_subscriber_calls_InvokeAsync()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        var sub1 = new Mock<IAsyncObserver<TopicMessage>>();
        sub1.Setup(_ => _.OnNextAsync(It.IsAny<TopicMessage>()))
            .Returns(async (TopicMessage msg) => await messageRouter.InvokeAsync("test-service"));
        var sub2 = new Mock<IAsyncObserver<TopicMessage>>();

        _connectionMock.Handle<InvokeRequest>(req => { }); // Swallow the request, let the caller wait forever

        await messageRouter.SubscribeAsync(
            "test-topic",
            sub1.Object);

        await messageRouter.SubscribeAsync(
            "test-topic",
            sub2.Object);

        await _connectionMock.SendToClient(
            new Protocol.Messages.TopicMessage
                {Topic = "test-topic", Payload = MessageBuffer.Create("payload1")});
        await _connectionMock.SendToClient(
            new Protocol.Messages.TopicMessage
                {Topic = "test-topic", Payload = MessageBuffer.Create("payload2")});

        await TaskExtensions.WaitForBackgroundTasksAsync();

        sub1.Verify(
            _ => _.OnNextAsync(
                It.Is<TopicMessage>(
                    msg => msg.Topic == "test-topic" && msg.Payload!.GetString() == "payload1")));
        sub2.Verify(
            _ => _.OnNextAsync(
                It.Is<TopicMessage>(
                    msg => msg.Topic == "test-topic" && msg.Payload!.GetString() == "payload1")));
        sub2.Verify(
            _ => _.OnNextAsync(
                It.Is<TopicMessage>(
                    msg => msg.Topic == "test-topic" && msg.Payload!.GetString() == "payload2")));
    }

    [Fact]
    public async Task Topic_extension_sends_a_Subscribe_message_on_first_subscription()
    {
        await using var messageRouter = CreateMessageRouter();

        var topic = messageRouter.Topic("test-topic");
        await using var sub1 = await topic.SubscribeAsync(_ => { });

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
        _connectionMock.Invocations.Clear();

        await using var sub2 = await topic.SubscribeAsync(_ => { });

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic", Times.Never);
    }

    [Fact]
    public async Task Topic_extension_sends_an_Unsubscribe_message_after_the_last_subscription_is_disposed()
    {
        await using var messageRouter = CreateMessageRouter();

        var topic = messageRouter.Topic("test-topic");
        var sub1 = await topic.SubscribeAsync(_ => { });
        var sub2 = await topic.SubscribeAsync(_ => { });
        await TaskExtensions.WaitForBackgroundTasksAsync();
        await sub1.DisposeAsync();
        await TaskExtensions.WaitForBackgroundTasksAsync();

        _connectionMock.Expect<UnsubscribeMessage>(msg => msg.Topic == "test-topic", Times.Never);

        await sub2.DisposeAsync();
        await TaskExtensions.WaitForBackgroundTasksAsync();

        _connectionMock.Expect<UnsubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
    }

    [Fact]
    public async Task When_the_last_subscription_is_disposed_it_sends_an_Unsubscribe_message()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var subscriber = new Mock<IAsyncObserver<TopicMessage>>();
        var sub1 = await messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        var sub2 = await messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        var sub3 = await messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        await sub1.DisposeAsync();
        await sub2.DisposeAsync();
        await TaskExtensions.WaitForBackgroundTasksAsync();

        _connectionMock.Expect<UnsubscribeMessage>(Times.Never);

        await sub3.DisposeAsync();
        await TaskExtensions.WaitForBackgroundTasksAsync();

        _connectionMock.Expect<UnsubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await messageRouter.InvokeAsync("test-service"));
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task InvokeAsync_sends_an_InvokeRequest_and_waits_for_an_InvokeResponse()
    {
        var invokeRequestReceived = new TaskCompletionSource<InvokeRequest>();
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        _connectionMock.Handle<InvokeRequest>(request => invokeRequestReceived.SetResult(request));

        var invokeTask = messageRouter.InvokeAsync("test-service", MessageBuffer.Create("test-payload"));

        invokeTask.IsCompleted.Should().BeFalse();

        var request = await invokeRequestReceived.Task;

        await _connectionMock.SendToClient(
            new InvokeResponse {RequestId = request.RequestId, Payload = MessageBuffer.Create("test-response")});

        var response = await invokeTask;

        response.Should().NotBeNull();
        response!.GetString().Should().Be("test-response");
    }

    [Fact]
    public async Task InvokeAsync_throws_if_the_InvokeResponse_contains_an_error()
    {
        var invokeRequestReceived = new TaskCompletionSource<InvokeRequest>();
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        _connectionMock.Handle((InvokeRequest request) => invokeRequestReceived.SetResult(request));

        var invokeTask = messageRouter.InvokeAsync("test-service", MessageBuffer.Create("test-payload"));

        invokeTask.IsCompleted.Should().BeFalse();

        var request = await invokeRequestReceived.Task;

        await _connectionMock.SendToClient(
            new InvokeResponse {RequestId = request.RequestId, Error = new Error("Error", "Invoke failed")});

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask);
        exception.Name.Should().Be("Error");
        exception.Message.Should().Be("Invoke failed");
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>()));

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task RegisterServiceAsync_Sends_a_RegisterService_request_and_waits_for_the_response()
    {
        var registerServiceRequestReceived = new TaskCompletionSource<RegisterServiceRequest>();
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        _connectionMock.Handle<RegisterServiceRequest>(request => registerServiceRequestReceived.SetResult(request));

        var registerServiceTask = messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>());

        registerServiceTask.IsCompleted.Should().BeFalse();
        var request = await registerServiceRequestReceived.Task;
        await _connectionMock.SendToClient(new RegisterServiceResponse {RequestId = request.RequestId});
        await registerServiceTask;
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_a_MessageRouterException_if_the_endpoint_is_already_registered()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        await messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>());

        var exception = await Assert.ThrowsAsync<MessageRouterException>(
            async () => await messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>()));
        exception.Name.Should().Be(MessageRouterErrors.DuplicateEndpoint);
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_if_the_response_contains_an_error()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>(
            request => new RegisterServiceResponse
            {
                RequestId =
                    request.RequestId,
                Error = new Error(MessageRouterErrors.DuplicateEndpoint, null)
            });

        var exception = await Assert.ThrowsAsync<MessageRouterException>(
            async () => await messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>()));

        exception.Name.Should().Be(MessageRouterErrors.DuplicateEndpoint);
    }

    [Fact]
    public async Task UnregisterServiceAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        var messageRouter = CreateMessageRouter();
        await messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await messageRouter.UnregisterServiceAsync("test-service"));

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task UnregisterServiceAsync_sends_an_UnregisterServiceRequest_and_waits_for_the_response()
    {
        var unregisterServiceRequestReceived = new TaskCompletionSource<UnregisterServiceRequest>();
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();

        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<UnregisterServiceRequest>(msg => unregisterServiceRequestReceived.SetResult(msg));

        await messageRouter.RegisterServiceAsync("test-service", Mock.Of<MessageHandler>());
        var unregisterServiceTask = messageRouter.UnregisterServiceAsync("test-service");

        unregisterServiceTask.IsCompleted.Should().BeFalse();

        var request = await unregisterServiceRequestReceived.Task;
        await _connectionMock.SendToClient(new UnregisterServiceResponse {RequestId = request.RequestId});
        await unregisterServiceTask;
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_invokes_the_registered_handler_and_responds_with_InvokeResponse()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));

        var messageHandler = new Mock<MessageHandler>();
        messageHandler.Setup(
                _ => _(
                    "test-service",
                    It.Is<MessageBuffer>(buf => buf.GetString() == "test-payload"),
                    It.Is<MessageContext>(
                        ctx => ctx.SourceId == "other-client" && ctx.CorrelationId == "test-correlation-id")))
            .ReturnsAsync(() => MessageBuffer.Create("test-response"))
            .Verifiable();

        await messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "test-service",
                Payload = MessageBuffer.Create("test-payload"),
                SourceId = "other-client",
                CorrelationId = "test-correlation-id"
            });

        var response = await invokeResponseReceived.Task;

        messageHandler.Verify();
        response.Payload!.GetString().Should().Be("test-response");
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_sends_an_InvokeResponse_with_error_if_the_handler_threw_an_exception()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));

        var messageHandler = new Mock<MessageHandler>();
        messageHandler.Setup(
                _ => _(
                    It.IsAny<string>(),
                    It.IsAny<MessageBuffer>(),
                    It.IsAny<MessageContext>()))
            .Callback(() => throw new InvalidOperationException("Invoke failed"));

        await messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "test-service",
                Payload = MessageBuffer.Create("test-payload"),
                SourceId = "other-client",
                CorrelationId = "test-correlation-id"
            });

        var response = await invokeResponseReceived.Task;
        response.Error.Should().NotBeNull();
        response.Error!.Message.Should().Be("Invoke failed");
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_sends_an_InvokeResponse_with_error_if_the_endpoint_is_not_registered()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));
        await messageRouter.ConnectAsync();

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "unknown-service",
            });

        var response = await invokeResponseReceived.Task;
        response.Error.Should().NotBeNull();
        response.Error!.Name.Should().Be(MessageRouterErrors.UnknownEndpoint);
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_repeatedly_calls_the_registered_handler_without_waiting_for_it_to_complete()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        var messageHandler = new Mock<MessageHandler>();
        messageHandler.Setup(_ => _("test-service", It.IsAny<MessageBuffer>(), It.IsAny<MessageContext>()))
            .Returns(() => new ValueTask<MessageBuffer?>(new TaskCompletionSource<MessageBuffer?>().Task));

        await messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "test-service",
            });

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "2",
                Endpoint = "test-service",
            });

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "3",
                Endpoint = "test-service",
            });

        await TaskExtensions.WaitForBackgroundTasksAsync();

        messageHandler.Verify(
            _ => _("test-service", It.IsAny<MessageBuffer>(), It.IsAny<MessageContext>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task When_the_connection_closes_it_calls_OnErrorAsync_on_active_subscribers()
    {
        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var subscriber = new Mock<IAsyncObserver<TopicMessage>>();
        await messageRouter.SubscribeAsync("test-topic", subscriber.Object);

        _connectionMock.Close(new MessageRouterException(MessageRouterErrors.ConnectionAborted, ""));

        await TaskExtensions.WaitForBackgroundTasksAsync();

        subscriber.Verify(
            _ => _.OnErrorAsync(
                It.Is<MessageRouterException>(e => e.Name == MessageRouterErrors.ConnectionAborted)));
    }

    [Fact]
    public async Task When_the_connection_closes_it_fails_pending_requests()
    {

        await using var messageRouter = CreateMessageRouter();
        await messageRouter.ConnectAsync();
        var invokeTask = messageRouter.InvokeAsync("test-service");

        _connectionMock.Close(new MessageRouterException(MessageRouterErrors.ConnectionAborted, ""));

        await TaskExtensions.WaitForBackgroundTasksAsync();

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask);
        exception.Name.Should().Be(MessageRouterErrors.ConnectionAborted);
    }

    public MessageRouterClientTests()
    {
        _connectionMock = new MockConnection();
        _connectionMock.AcceptConnections();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private readonly MockConnection _connectionMock;
    private static readonly MessageRouterOptions DefaultOptions = new MessageRouterOptions();

    private MessageRouterClient CreateMessageRouter(MessageRouterOptions? options = null)
    {
        var connectionFactory = new Mock<IConnectionFactory>();
        connectionFactory.Setup(_ => _.CreateConnection()).Returns(_connectionMock.Object);

        return new MessageRouterClient(connectionFactory.Object, options ?? DefaultOptions);
    }
}