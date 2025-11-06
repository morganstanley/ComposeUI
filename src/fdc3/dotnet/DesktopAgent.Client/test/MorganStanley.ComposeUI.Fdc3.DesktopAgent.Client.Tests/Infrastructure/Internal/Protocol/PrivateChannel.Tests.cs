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
using Finos.Fdc3.Context;
using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal.Protocol;

public class PrivateChannelTests
{
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly string _channelId = "test-channel";
    private readonly string _instanceId = "test-instance";
    private readonly DisplayMetadata _displayMetadata = new();
    private readonly PrivateChannel _channel;
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    public PrivateChannelTests()
    {
        _messagingMock.Setup(m => m.SubscribeAsync(It.IsAny<string>(), It.IsAny<TopicMessageHandler>(), It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _messagingMock.Setup(m => m.RegisterServiceAsync(It.IsAny<string>(), It.IsAny<ServiceHandler>(), It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _messagingMock.Setup(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());

        _channel = new PrivateChannel(
            _channelId,
            _messagingMock.Object,
            _instanceId,
            onDisconnect: () => { },
            isOriginalCreator: true);
    }

    [Fact]
    public async Task Broadcast_when_disconnected_throws()
    {
        _channel.Disconnect();

        await Task.Delay(2000);

        Func<Task> act = async () => await _channel.Broadcast(Mock.Of<IContext>());

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*disconnected*");
    }

    [Fact]
    public async Task Broadcast_when_connected_calls_publish()
    {
        var context = new Instrument();

        await _channel.Broadcast(context);

        _messagingMock.Verify(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task AddContextListener_when_disconnected_throws()
    {
        _channel.Disconnect();

        Func<Task> act = async () => await _channel.AddContextListener<Instrument>(null, (ctx, ctxM) => { });

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*disconnected*");
    }

    [Fact]
    public async Task AddContextListener_when_connected_returns_listener()
    {
        var response = new AddContextListenerResponse
        {
            Success = true,
            Id = "test-listener-id"
        };

        _messagingMock.Setup(
            _ => _.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var channel = new PrivateChannel(_channelId, _messagingMock.Object, _instanceId, onDisconnect:() => { }, isOriginalCreator: true);

        var listener = await channel.AddContextListener<Instrument>(null, (ctx, ctxM) => { });

        listener.Should().NotBeNull();
    }

    [Fact]
    public void OnAddContextListener_when_connected_returns_listener()
    {
        var listener = _channel.OnAddContextListener(_ => { });

        listener.Should().NotBeNull();
    }

    [Fact]
    public async Task OnAddContextListener_when_disconnected_throws()
    {
        _channel.Disconnect();

        await Task.Delay(2000);

        Action act = () => _channel.OnAddContextListener(_ => { });

        act.Should().Throw<Fdc3DesktopAgentException>().WithMessage("*disconnected*");
    }

    [Fact]
    public void OnDisconnect_when_connected_returns_listener()
    {
        var listener = _channel.OnDisconnect(() => { });

        listener.Should().NotBeNull();
    }

    [Fact]
    public void OnDisconnect_when_disconnected_throws()
    {
        _channel.Disconnect();

        Action act = () => _channel.OnDisconnect(() => { });

        act.Should().Throw<Fdc3DesktopAgentException>().WithMessage("*disconnected*");
    }

    [Fact]
    public void OnUnsubscribe_when_connected_returns_listener()
    {
        var listener = _channel.OnUnsubscribe(_ => { });

        listener.Should().NotBeNull();
    }

    [Fact]
    public async Task OnUnsubscribe_when_disconnected_throws()
    {
        _channel.Disconnect();

        await Task.Delay(2000);

        Action act = () => _channel.OnUnsubscribe(_ => { });

        act.Should().Throw<Fdc3DesktopAgentException>().WithMessage("*disconnected*");
    }

    [Fact]
    public void Disconnect_when_called_multiple_times_does_not_throw()
    {
        _channel.Disconnect();
        Action act = () => _channel.Disconnect();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_when_called_disposes_resources()
    {
        Func<Task> act = async () => await _channel.DisposeAsync();

        await act.Should().NotThrowAsync();
    }
}
