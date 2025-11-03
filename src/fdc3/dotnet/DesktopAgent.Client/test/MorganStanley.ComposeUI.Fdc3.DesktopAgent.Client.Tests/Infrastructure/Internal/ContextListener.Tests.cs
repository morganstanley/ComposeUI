/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Text.Json;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class ContextListenerTests
{
    private const string InstanceId = "testInstance";
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly ContextHandler<IContext> _contextHandler;
    private readonly ContextListener<IContext> _listener;
    private bool _handlerCalled;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public ContextListenerTests()
    {
        _contextHandler = (context, metadata) => { _handlerCalled = true; };

        _listener = new ContextListener<IContext>(
            InstanceId,
            _contextHandler,
            _messagingMock.Object,
            "fdc3,instrument");
        _handlerCalled = false;
    }

    [Fact]
    public void Unsubscribe_does_nothing_when_not_subscribed_on_the_context_listener()
    {
        var act = () => _listener.Unsubscribe();

        act.Should().NotThrow();

        _messagingMock.Verify(
            m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Unsubscribe_unregisters_and_disposes_when_subscribed_on_the_context_listener()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        var disposableMock = new Mock<IAsyncDisposable>();
        disposableMock.Setup(d => d.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _messagingMock
            .Setup(m => m.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(disposableMock.Object);

        var removeResponse = new RemoveContextListenerResponse
        {
            Success = true
        };

        _messagingMock
            .SetupSequence(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(removeResponse, _jsonSerializerOptions)));

        await _listener.SubscribeAsync("channelId", ChannelType.App);

        var act = () => _listener.Unsubscribe();

        act.Should().NotThrow();
        disposableMock.Verify(d => d.DisposeAsync(), Times.Once);

        _messagingMock.Verify(
            m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Unsubscribe_handles_exception_during_unregister_on_the_context_listener()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        var disposableMock = new Mock<IAsyncDisposable>();
        disposableMock.Setup(d => d.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _messagingMock
            .Setup(m => m.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(disposableMock.Object);

        _messagingMock
            .SetupSequence(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)))
            .ThrowsAsync(new InvalidOperationException("Unregister error"));

        await _listener.SubscribeAsync("channelId", ChannelType.App);

        var act = () => _listener.Unsubscribe();
        act.Should().Throw<InvalidOperationException>().WithMessage("Unregister error");
    }

    [Fact]
    public async Task Unsubscribe_handles_exception_during_dispose_on_the_context_listener()
    {
        var response = new AddContextListenerResponse
        {
            Success = true,
            Id = Guid.NewGuid().ToString()
        };

        var disposableMock = new Mock<IAsyncDisposable>();
        disposableMock.Setup(d => d.DisposeAsync()).Throws(new InvalidOperationException("Dispose error"));

        _messagingMock
            .Setup(m => m.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(disposableMock.Object);

        var removeResponse = new RemoveContextListenerResponse
        {
            Success = true
        };

        _messagingMock
            .SetupSequence(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(removeResponse, _jsonSerializerOptions)));

        await _listener.SubscribeAsync("channelId", ChannelType.App);

        Action act = () => _listener.Unsubscribe();
        act.Should().Throw<InvalidOperationException>().WithMessage("Dispose error");
    }

    [Fact]
    public async Task SubscribeAsync_subscribes_and_invokes_handler_when_not_subscribed()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var disposableMock = new Mock<IAsyncDisposable>();
        disposableMock.Setup(d => d.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _messagingMock
            .Setup(m => m.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(disposableMock.Object);

        await _listener.SubscribeAsync("channelId", ChannelType.App);

        _messagingMock.Invocations.Should().ContainSingle(inv =>
            inv.Method.Name == nameof(IMessaging.InvokeServiceAsync));

        _messagingMock.Invocations.Should().ContainSingle(inv =>
            inv.Method.Name == nameof(IMessaging.SubscribeAsync));
    }

    [Fact]
    public async Task SubscribeAsync_does_not_resubscribe_when_already_subscribed()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var disposableMock = new Mock<IAsyncDisposable>();
        disposableMock.Setup(d => d.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _messagingMock
            .Setup(m => m.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(disposableMock.Object);

        await _listener.SubscribeAsync("channelId", ChannelType.App);
        await _listener.SubscribeAsync("channelId", ChannelType.App);

        _messagingMock.Invocations.Should().ContainSingle(inv =>
            inv.Method.Name == nameof(IMessaging.InvokeServiceAsync));

        _messagingMock.Invocations.Should().ContainSingle(inv =>
            inv.Method.Name == nameof(IMessaging.SubscribeAsync));
    }

    [Fact]
    public async Task SubscribeAsync_returns_error_when_messaging_throws()
    {
        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Messaging error"));

        var act = async() => await _listener.SubscribeAsync("channelId", ChannelType.App);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SubscribeAsync_returns_error_when_response_is_null()
    {
        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?)null));

        var act = async () => await _listener.SubscribeAsync("channelId", ChannelType.App);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task SubscribeAsync_returns_error_when_response_has_error()
    {
        var response = new AddContextListenerResponse
        {
            Error = "Some error",
            Success = false,
            Id = null
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var act = async() => await _listener.SubscribeAsync("channelId", ChannelType.App);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task SubscribeAsync_returns_error_when_response_is_unsuccessful()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = false,
            Id = null
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var act = async () => await _listener.SubscribeAsync("channelId", ChannelType.App);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*app was not able to register its context listener for channel*");
    }

    [Fact]
    public async Task SubscribeAsync_returns_error_when_response_id_is_null()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = null
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var act = async () => await _listener.SubscribeAsync("channelId", ChannelType.App);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No successful response containing the subscription ID was received from the FDC3 backend server while registering the context listener.*");
    }

    [Fact]
    public async Task HandleContextAsync_invokes_handler_when_context_type_matches_and_is_subscribed()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var handlerCalled = false;
        var handler = new ContextHandler<Instrument>((ctx, _) => handlerCalled = true);
        var listener = new ContextListener<Instrument>("instanceId", handler, _messagingMock.Object, "fdc3.instrument");

        await listener.SubscribeAsync("channelId", ChannelType.App);

        var context = new Instrument(new InstrumentID { Ticker = Guid.NewGuid().ToString() }, Guid.NewGuid().ToString());

        await listener.HandleContextAsync(context);

        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleContextAsync_returns_error_when_not_subscribed()
    {
        var handler = new ContextHandler<Instrument>((ctx, _) => { });
        var messagingMock = new Mock<IMessaging>();
        var listener = new ContextListener<Instrument>("instanceId", handler, messagingMock.Object, "testType");
        var context = new Instrument(new InstrumentID { Ticker = Guid.NewGuid().ToString() }, Guid.NewGuid().ToString());

        var act = () => listener.HandleContextAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The context listener is not subscribed to any channel.");
    }

    [Fact]
    public async Task HandleContextAsync_does_not_invoke_handler_when_context_type_does_not_match()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var handlerCalled = false;
        var handler = new ContextHandler<Instrument>((ctx, _) => handlerCalled = true);
        var listener = new ContextListener<Instrument>("instanceId", handler, _messagingMock.Object, "testType");

        await listener.SubscribeAsync("channelId", ChannelType.App);

        var context = new MyContext() { Type = "fdc3.instrument" };

        await listener.HandleContextAsync(context);

        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleContextAsync_does_not_invoke_handler_when_context_is_not_expected_type()
    {
        var response = new AddContextListenerResponse
        {
            Error = null,
            Success = true,
            Id = "listenerId"
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var handlerCalled = false;
        var handler = new ContextHandler<Instrument>((ctx, _) => handlerCalled = true);

        var listener = new ContextListener<Instrument>("instanceId", handler, _messagingMock.Object, "testType");

        await listener.SubscribeAsync("channelId", ChannelType.App);

        var context = new Contact(new ContactID() { Email = "test@email.com" }, "test-name");

        await listener.HandleContextAsync(context);

        handlerCalled.Should().BeFalse();
    }

    private class MyContext : IContext
    {
        public object? ID { get; set; }

        public string? Name { get; set; }

        public string Type { get; set; }

        public dynamic? Native { get; set; }
    }
}
