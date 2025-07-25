﻿// Morgan Stanley makes this available to you under the Apache License,
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
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Instrumentation;
using MorganStanley.ComposeUI.Messaging.Protocol;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.TestUtils;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging.Client;

public class MessageRouterClientTests : IAsyncLifetime
{
    [Fact]
    public async Task DisposeAsync_does_not_invoke_the_connection_when_called_before_connecting()
    {
        await _messageRouter.DisposeAsync();

        await WaitForCompletionAsync();
        _connectionMock.Verify(_ => _.DisposeAsync());
        _connectionMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_connection()
    {
        await _messageRouter.ConnectAsync();
        await _messageRouter.DisposeAsync();

        await WaitForCompletionAsync();
        _connectionMock.Verify(_ => _.DisposeAsync());
    }

    [Fact]
    public async Task DisposeAsync_does_not_throw_if_the_client_was_already_closed()
    {
        await _messageRouter.DisposeAsync();
        await _messageRouter.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_connection_exactly_once()
    {
        await _messageRouter.DisposeAsync();
        await _messageRouter.DisposeAsync();

        await WaitForCompletionAsync();
        _connectionMock.Verify(_ => _.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_completes_pending_requests_with_a_MessageRouterException()
    {
        await _messageRouter.ConnectAsync();
        var invokeTask = _messageRouter.InvokeAsync("test-endpoint").AsTask();
        await _messageRouter.DisposeAsync();

        await WaitForCompletionAsync();
        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask).WaitAsync(TestTimeout);
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task ConnectAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(async () => await _messageRouter.ConnectAsync()).WaitAsync(TestTimeout);
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task ConnectAsync_sends_a_ConnectRequest_and_waits_for_a_ConnectResponse()
    {
        var connectRequestReceived = new TaskCompletionSource();
        _connectionMock.Handle<ConnectRequest>(_ => connectRequestReceived.SetResult());

        var connectTask = _messageRouter.ConnectAsync().AsTask();
        await connectRequestReceived.Task;

        connectTask.IsCompleted.Should().BeFalse();

        await _connectionMock.SendToClient(new ConnectResponse { ClientId = "client-id" });
        await connectTask.WaitAsync(TestTimeout);

        _messageRouter.ClientId.Should().Be("client-id");
    }

    [Fact]
    public async Task ConnectAsync_throws_a_MessageRouterException_if_the_ConnectResponse_contains_an_error()
    {
        var connectRequestReceived = new TaskCompletionSource();
        _connectionMock.Handle<ConnectRequest>(_ => connectRequestReceived.SetResult());

        var connectTask = _messageRouter.ConnectAsync();
        await connectRequestReceived.Task.WaitAsync(TestTimeout);

        connectTask.IsCompleted.Should().BeFalse();

        await _connectionMock.SendToClient(new ConnectResponse { Error = new Error("Error", "Fail") });

        await WaitForCompletionAsync();

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await connectTask).WaitAsync(TestTimeout);
        exception.Name.Should().Be("Error");
        exception.Message.Should().Be("Fail");
    }

    [Fact]
    public async Task PublishAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.PublishAsync("test-topic", MessageBuffer.Create(""))).WaitAsync(TestTimeout);

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task PublishAsync_sends_a_PublishMessage()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<PublishMessage, PublishResponse>();
        _diagnosticObserver.ExpectMessage<PublishMessage>();

        await _messageRouter.PublishAsync(
            "test-topic",
           MessageBuffer.Create("test-payload"),
            new PublishOptions
            { CorrelationId = "test-correlation-id" });


        await WaitForCompletionAsync();

        _connectionMock.Expect<PublishMessage>(
            msg => msg.Topic == "test-topic"
                   && msg.Payload != null
                   && msg.Payload.GetString() == "test-payload"
                   && msg.CorrelationId == "test-correlation-id");
    }

    [Fact]
    public async Task PublishAsync_throws_if_PublishResponse_contains_Error()
    {
        await _messageRouter.ConnectAsync();

        async ValueTask SendPublishResponse(string requestId)
        {
            await _connectionMock.SendToClient(new PublishResponse() { RequestId = requestId, Error = new Error("testError-publish", null) });
        }

        _connectionMock.Handle<PublishMessage>(
            async request => await SendPublishResponse(request.RequestId));

        _diagnosticObserver.ExpectMessage<PublishMessage>();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.PublishAsync(
                    "test-topic",
                    MessageBuffer.Create("test-payload"),
                    new PublishOptions
                    { CorrelationId = "test-correlation-id" }));

        exception.Name.Should().Be("testError-publish");
    }

    [Fact]
    public async Task SubscribeAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.SubscribeAsync(
                    "test-topic",
                    new Mock<MessageHandler>().Object)).WaitAsync(TestTimeout);

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task SubscribeAsync_sends_a_Subscribe_message()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();
        _diagnosticObserver.ExpectMessage<SubscribeMessage>();

        await _messageRouter.SubscribeAsync("test-topic", new Mock<MessageHandler>().Object);

        await WaitForCompletionAsync();

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic");
    }

    [Fact]
    public async Task SubscribeAsync_only_sends_a_Subscribe_message_on_the_first_subscription()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();

        _diagnosticObserver.ExpectMessage<SubscribeMessage>();
        await _messageRouter.SubscribeAsync("test-topic", new Mock<MessageHandler>().Object);
        await _messageRouter.SubscribeAsync("test-topic", new Mock<MessageHandler>().Object);
        await _messageRouter.SubscribeAsync("test-topic", new Mock<MessageHandler>().Object);

        await WaitForCompletionAsync();

        _connectionMock.Expect<SubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_throws_if_SubscribeResponse_contains_Error()
    {
        await _messageRouter.ConnectAsync();

        async ValueTask SendSubscribeResponse(string requestId)
        {
            await _connectionMock.SendToClient(new SubscribeResponse() { RequestId = requestId, Error = new Error("testError-subscribe", null) });
        }

        _connectionMock.Handle<SubscribeMessage>(
            async request => await SendSubscribeResponse(request.RequestId));

        _diagnosticObserver.ExpectMessage<SubscribeMessage>();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.SubscribeAsync("test-topic", new Mock<MessageHandler>().Object));

        exception.Name.Should().Be("testError-subscribe");
    }

    [Fact]
    public async Task When_Topic_message_received_it_invokes_the_subscribers()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();

        var sub1 = new Mock<MessageHandler>();
        var sub2 = new Mock<MessageHandler>();
        await _messageRouter.SubscribeAsync("test-topic", sub1.Object);
        await _messageRouter.SubscribeAsync("test-topic", sub2.Object);

        await _connectionMock.SendToClient(
            RegisterRequest(
                new Protocol.Messages.TopicMessage
                {
                    Topic = "test-topic",
                    Payload = MessageBuffer.Create("test-payload"),
                    SourceId = "other-client",
                    CorrelationId = "test-correlation-id",
                }));

        await WaitForCompletionAsync();

        sub1.Verify(x => x(It.Is<IMessageBuffer>(b => b.GetString() == "test-payload")), Times.Once());
        sub2.Verify(x => x(It.Is<IMessageBuffer>(b => b.GetString() == "test-payload")), Times.Once());

    }

    [Fact]
    public async Task When_Topic_message_received_it_keeps_processing_messages_if_the_subscriber_calls_InvokeAsync()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();

        // Register two subscribers, the first one will invoke a service that completes when
        // the second subscriber has been called twice. If the first subscriber could block
        // the pipeline with the InvokeAsync call, this test would fail, because the second
        // subscriber would never get the second message.

        var countdown = new AsyncCountdownEvent(2);
        _connectionMock.Handle<InvokeRequest>(_ => new ValueTask(countdown.WaitAsync()));

        var sub1 = new Mock<MessageHandler>();
        sub1.Setup(x => x(It.IsAny<IMessageBuffer>()))
            .Returns(async (IMessageBuffer msg) => { await _messageRouter.InvokeAsync("test-service"); return; });

        var sub2 = new Mock<MessageHandler>();
        sub1.Setup(x => x(It.IsAny<IMessageBuffer>()))
            .Returns((IMessageBuffer msg) =>
            {
                countdown.Signal();
                return new ValueTask();
            });

        await _messageRouter.SubscribeAsync(
            "test-topic",
            sub1.Object);

        await _messageRouter.SubscribeAsync(
            "test-topic",
            sub2.Object);

        await _connectionMock.SendToClient(
            RegisterRequest(
                new Protocol.Messages.TopicMessage
                { Topic = "test-topic", Payload = MessageBuffer.Create("payload1") }));

        await _connectionMock.SendToClient(
            RegisterRequest(
                new Protocol.Messages.TopicMessage
                { Topic = "test-topic", Payload = MessageBuffer.Create("payload2") }));

        await WaitForCompletionAsync();

        sub1.Verify(_ => _(It.Is<IMessageBuffer>(msg => msg.GetString() == "payload1")));
        sub2.Verify(_ => _(It.Is<IMessageBuffer>(msg => msg.GetString() == "payload1")));
        sub2.Verify(_ => _(It.Is<IMessageBuffer>(msg => msg.GetString() == "payload2")));
    }

    [Fact]
    public async Task When_the_last_subscription_is_disposed_it_sends_an_Unsubscribe_message()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();
        var subscriber = new Mock<MessageHandler>();
        var sub1 = await _messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        var sub2 = await _messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        var sub3 = await _messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        await sub1.DisposeAsync();
        await sub2.DisposeAsync();
        await WaitForCompletionAsync();

        _connectionMock.Expect<UnsubscribeMessage>(Times.Never);

        _diagnosticObserver.ExpectMessage<UnsubscribeMessage>();
        await sub3.DisposeAsync();
        await WaitForCompletionAsync();

        _connectionMock.Expect<UnsubscribeMessage>(msg => msg.Topic == "test-topic", Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.InvokeAsync("test-service"));
        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task InvokeAsync_sends_an_InvokeRequest_and_waits_for_an_InvokeResponse()
    {
        var invokeRequestReceived = new TaskCompletionSource<InvokeRequest>();
        await _messageRouter.ConnectAsync();

        _connectionMock.Handle<InvokeRequest>(request => invokeRequestReceived.SetResult(request));

        var invokeTask = _messageRouter.InvokeAsync("test-service", MessageBuffer.Create("test-payload")).AsTask();

        invokeTask.IsCompleted.Should().BeFalse();

        var request = await invokeRequestReceived.Task;

        await _connectionMock.SendToClient(
            new InvokeResponse { RequestId = request.RequestId, Payload = MessageBuffer.Create("test-response") });

        var response = await invokeTask.WaitAsync(TestTimeout);

        response.Should().NotBeNull();
        response!.GetString().Should().Be("test-response");
    }

    [Fact]
    public async Task InvokeAsync_throws_if_the_InvokeResponse_contains_an_error()
    {
        var invokeRequestReceived = new TaskCompletionSource<InvokeRequest>();
        await _messageRouter.ConnectAsync();

        _connectionMock.Handle((InvokeRequest request) => invokeRequestReceived.SetResult(request));

        var invokeTask = _messageRouter.InvokeAsync("test-service", MessageBuffer.Create("test-payload")).AsTask();

        invokeTask.IsCompleted.Should().BeFalse();

        var request = await invokeRequestReceived.Task.WaitAsync(TestTimeout);

        await _connectionMock.SendToClient(
            new InvokeResponse { RequestId = request.RequestId, Error = new Error("Error", "Invoke failed") });

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask);
        exception.Name.Should().Be("Error");
        exception.Message.Should().Be("Invoke failed");
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>()));

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task RegisterServiceAsync_Sends_a_RegisterService_request_and_waits_for_the_response()
    {
        var registerServiceRequestReceived = new TaskCompletionSource<RegisterServiceRequest>();
        await _messageRouter.ConnectAsync();

        _connectionMock.Handle<RegisterServiceRequest>(request => registerServiceRequestReceived.SetResult(request));

        var registerServiceTask = _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>()).AsTask();

        registerServiceTask.IsCompleted.Should().BeFalse();
        var request = await registerServiceRequestReceived.Task.WaitAsync(TestTimeout);
        await _connectionMock.SendToClient(new RegisterServiceResponse { RequestId = request.RequestId });
        await registerServiceTask.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_a_MessageRouterException_if_the_endpoint_is_already_registered()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        await _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>());

        var exception = await Assert.ThrowsAsync<MessageRouterDuplicateEndpointException>(
            async () => await _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>())).WaitAsync(TestTimeout);

        exception.Name.Should().Be(MessageRouterErrors.DuplicateEndpoint);
    }

    [Fact]
    public async Task RegisterServiceAsync_throws_if_the_response_contains_an_error()
    {
        await _messageRouter.ConnectAsync();

        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>(
            request => new RegisterServiceResponse
            {
                RequestId = request.RequestId,
                Error = new Error(MessageRouterErrors.DuplicateEndpoint, message: null)
            });

        var exception = await Assert.ThrowsAsync<MessageRouterException>(
            async () => await _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>())).WaitAsync(TestTimeout);

        exception.Name.Should().Be(MessageRouterErrors.DuplicateEndpoint);
    }

    [Fact]
    public async Task UnregisterServiceAsync_throws_a_MessageRouterException_if_the_client_was_previously_closed()
    {
        await _messageRouter.DisposeAsync();

        var exception =
            await Assert.ThrowsAsync<MessageRouterException>(
                async () => await _messageRouter.UnregisterServiceAsync("test-service"));

        exception.Name.Should().Be(MessageRouterErrors.ConnectionClosed);
    }

    [Fact]
    public async Task UnregisterServiceAsync_sends_an_UnregisterServiceRequest_and_waits_for_the_response()
    {
        var unregisterServiceRequestReceived = new TaskCompletionSource<UnregisterServiceRequest>();
        await _messageRouter.ConnectAsync();

        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<UnregisterServiceRequest>(msg => unregisterServiceRequestReceived.SetResult(msg));

        await _messageRouter.RegisterServiceAsync("test-service", Mock.Of<ServiceHandler>());
        var unregisterServiceTask = _messageRouter.UnregisterServiceAsync("test-service").AsTask();

        unregisterServiceTask.IsCompleted.Should().BeFalse();

        var request = await unregisterServiceRequestReceived.Task.WaitAsync(TestTimeout);
        await _connectionMock.SendToClient(new UnregisterServiceResponse { RequestId = request.RequestId });
        await unregisterServiceTask;
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_invokes_the_registered_handler_and_responds_with_InvokeResponse()
    {
        await _messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));

        var messageHandler = new Mock<ServiceHandler>();
        messageHandler.Setup(
                _ => _(
                    "test-service",
                    It.Is<MessageBuffer>(buf => buf.GetString() == "test-payload"),
                    It.Is<MessageContext>(
                        ctx => ctx.SourceId == "other-client" && ctx.CorrelationId == "test-correlation-id")))
            .ReturnsAsync(() => MessageBuffer.Create("test-response"))
            .Verifiable();

        await _messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service",
                    Payload = MessageBuffer.Create("test-payload"),
                    SourceId = "other-client",
                    CorrelationId = "test-correlation-id"
                }));

        var response = await invokeResponseReceived.Task;

        messageHandler.Verify();
        response.Payload!.GetString().Should().Be("test-response");
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_sends_an_InvokeResponse_with_error_if_the_handler_threw_an_exception()
    {
        await _messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));

        var messageHandler = new Mock<ServiceHandler>();
        messageHandler.Setup(
                _ => _(
                    It.IsAny<string>(),
                    It.IsAny<MessageBuffer>(),
                    It.IsAny<MessageContext>()))
            .Callback(() => throw new InvalidOperationException("Invoke failed"));

        await _messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "test-service",
                Payload = MessageBuffer.Create("test-payload"),
                SourceId = "other-client",
                CorrelationId = "test-correlation-id"
            });

        var response = await invokeResponseReceived.Task.WaitAsync(TestTimeout);
        response.Error.Should().NotBeNull();
        response.Error!.Message.Should().Be("Invoke failed");
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_sends_an_InvokeResponse_with_error_if_the_endpoint_is_not_registered()
    {
        await _messageRouter.ConnectAsync();
        var invokeResponseReceived = new TaskCompletionSource<InvokeResponse>();
        _connectionMock.Handle<InvokeResponse>(msg => invokeResponseReceived.SetResult(msg));
        await _messageRouter.ConnectAsync();

        await _connectionMock.SendToClient(
            new InvokeRequest
            {
                RequestId = "1",
                Endpoint = "unknown-service",
            });

        var response = await invokeResponseReceived.Task.WaitAsync(TestTimeout);
        response.Error.Should().NotBeNull();
        response.Error!.Name.Should().Be(MessageRouterErrors.UnknownEndpoint);
    }

    [Fact]
    public async Task
        When_responding_to_Invoke_messages_it_repeatedly_calls_the_registered_handler_without_waiting_for_it_to_complete()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<RegisterServiceRequest, RegisterServiceResponse>();
        var countdown = new AsyncCountdownEvent(3);
        var messageHandler = new Mock<ServiceHandler>();
        messageHandler.Setup(_ => _("test-service", It.IsAny<MessageBuffer>(), It.IsAny<MessageContext>()))
            .Returns(async () =>
            {
                countdown.Signal();
                await countdown.WaitAsync();

                return null;
            });

        await _messageRouter.RegisterServiceAsync("test-service", messageHandler.Object);

        await _connectionMock.SendToClient(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "1",
                    Endpoint = "test-service",
                }));

        await _connectionMock.SendToClient(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "2",
                    Endpoint = "test-service",
                }));

        await _connectionMock.SendToClient(
            RegisterRequest(
                new InvokeRequest
                {
                    RequestId = "3",
                    Endpoint = "test-service",
                }));

        await WaitForCompletionAsync();

        messageHandler.Verify(
            _ => _("test-service", It.IsAny<MessageBuffer>(), It.IsAny<MessageContext>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task When_the_connection_closes_it_fails_pending_requests()
    {
        await _messageRouter.ConnectAsync();
        var invokeTask = _messageRouter.InvokeAsync("test-service");

        _connectionMock.Close(new MessageRouterException(MessageRouterErrors.ConnectionAborted, ""));

        await WaitForCompletionAsync();

        var exception = await Assert.ThrowsAsync<MessageRouterException>(async () => await invokeTask);
        exception.Name.Should().Be(MessageRouterErrors.ConnectionAborted);
    }

    [Fact]
    public async Task When_a_subscription_is_disposed_there_will_be_no_further_calls_to_the_subscriber()
    {
        await _messageRouter.ConnectAsync();
        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();
        IAsyncDisposable subscription = null!;
        var subscriber = new Mock<MessageHandler>();

        subscriber.Setup(_ => _(It.IsAny<IMessageBuffer>()))
            .Returns(
                async (IMessageBuffer msg) =>
                {
                    if (msg.GetString() == "2")
                    {
                        await subscription.DisposeAsync().AsTask().WaitAsync(TestTimeout);
                    }
                });

        subscription = await _messageRouter.SubscribeAsync("test-topic", subscriber.Object);
        await WaitForCompletionAsync();
        await _connectionMock.SendToClient(
            RegisterRequest(new Protocol.Messages.TopicMessage { Topic = "test-topic", Payload = MessageBuffer.Create("1") }));
        await _connectionMock.SendToClient(
            RegisterRequest(new Protocol.Messages.TopicMessage { Topic = "test-topic", Payload = MessageBuffer.Create("2") }));
        await _connectionMock.SendToClient(
            RegisterRequest(new Protocol.Messages.TopicMessage { Topic = "test-topic", Payload = MessageBuffer.Create("3") }));
        await WaitForCompletionAsync();

        subscriber.Verify(_ => _(It.Is<IMessageBuffer>(msg => msg.GetString() == "1")), Times.Once);
        subscriber.Verify(_ => _(It.Is<IMessageBuffer>(msg => msg.GetString() == "2")), Times.Once);
        subscriber.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task It_log_error_when_a_subscription_is_disposed_and_the_UnsubscribeResponse_contains_Error()
    {
        await _messageRouter.ConnectAsync();

        async ValueTask SendUnsubscribeResponse(string requestId)
        {
            await _connectionMock.SendToClient(
                new UnsubscribeResponse { RequestId = requestId, Error = new Error("testError-unsubscribe", null) });
        }

        _connectionMock.Handle<SubscribeMessage, SubscribeResponse>();
        _connectionMock.Handle<UnsubscribeMessage>(
            request => SendUnsubscribeResponse(request.RequestId));

        IAsyncDisposable subscription = null!;
        var subscriber = new Mock<MessageHandler>();

        _diagnosticObserver.ExpectMessage<UnsubscribeMessage>();

        subscription = await _messageRouter.SubscribeAsync("test-topic", subscriber.Object);

        await subscription.DisposeAsync();

        await WaitForCompletionAsync();

        Thread.Sleep(1);

        _loggerMock
            .Verify(
                _ => _.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((message, _) => message.ToString()!.Contains("Exception thrown while unsubscribing, topic: test-topic")),
                    It.IsAny<MessageRouterException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);
    }

    public MessageRouterClientTests()
    {
        _connectionMock = new MockConnection();
        _connectionMock.AcceptConnections();
        var connectionFactory = new Mock<IConnectionFactory>();
        connectionFactory.Setup(_ => _.CreateConnection()).Returns(_connectionMock.Object);
        _loggerMock = new Mock<ILogger<MessageRouterClient>>();
        _loggerMock.Setup(_ => _.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _messageRouter = new MessageRouterClient(connectionFactory.Object, new MessageRouterOptions(), _loggerMock.Object);
        _diagnosticObserver = new MessageRouterDiagnosticObserver(_messageRouter);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(1);

    private readonly MockConnection _connectionMock;
    private readonly MessageRouterClient _messageRouter;
    private readonly MessageRouterDiagnosticObserver _diagnosticObserver;
    private readonly Mock<ILogger<MessageRouterClient>> _loggerMock;

    private TMessage RegisterRequest<TMessage>(TMessage message) where TMessage : Message
    {
        return _diagnosticObserver.RegisterRequest(message);
    }

    private async Task WaitForCompletionAsync()
    {
        await _diagnosticObserver.WaitForCompletionAsync(TestTimeout);
    }
}