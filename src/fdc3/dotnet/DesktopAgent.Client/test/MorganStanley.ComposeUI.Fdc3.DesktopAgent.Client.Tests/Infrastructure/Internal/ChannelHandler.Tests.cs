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

public class ChannelHandlerTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    [Fact]
    public async Task CreateContextListenerAsync_returns_listener_from_channel_when_currentChannel_is_provided()
    {
        var messagingMock = new Mock<IMessaging>();
        var channelMock = new Mock<IChannel>();
        var handler = new ContextHandler<Instrument>((ctx, _)=> { });
        var expectedListener = new ContextListener<Instrument>("instanceId", handler, messagingMock.Object, "fdc3.instrument");

        var desktopAgentClientMock = new Mock<IDesktopAgent>();

        channelMock
            .Setup(c => c.AddContextListener("fdc3.instrument", handler))
            .ReturnsAsync(expectedListener);

        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var result = await factory.CreateContextListenerAsync(handler, channelMock.Object, "fdc3.instrument");

        result.Should().BeSameAs(expectedListener);
    }

    [Fact]
    public async Task CreateContextListenerAsync_creates_new_listener_when_currentChannel_is_null()
    {
        var messagingMock = new Mock<IMessaging>();
        var handler = new ContextHandler<Instrument>((ctx, _) => { });
        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var result = await factory.CreateContextListenerAsync(handler, null, "fdc3.instrument");

        result.Should().BeOfType<ContextListener<Instrument>>();
    }

    [Fact]
    public async Task JoinUserChannelAsync_returns_channel_when_successful()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new JoinUserChannelResponse
        {
            Success = true,
            Error = null
        };

        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var result = await factory.JoinUserChannelAsync("channelId");

        result.Id.Should().Be("channelId");
        result.Type.Should().Be(ChannelType.User);
    }

    [Fact]
    public async Task JoinUserChannelAsync_returns_error_when_response_is_null()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
        _ => _.InvokeServiceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .Returns(new ValueTask<string?>((string?) null));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.JoinUserChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task JoinUserChannelAsync_returns_error_when_response_has_error()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new JoinUserChannelResponse
        {
            Success = false,
            Error = "Some error"
        };

        messagingMock.Setup(
            _ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
        .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.JoinUserChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task JoinUserChannelAsync_returns_error_when_response_is_unsuccessful()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new JoinUserChannelResponse
        {
            Success = false,
            Error = null
        };

        messagingMock.Setup(
            _ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
        .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async() => await factory.JoinUserChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*app was not able to join to user channel: channelId*");
    }

    [Fact]
    public async Task JoinUserChannelAsync_returns_error_when_messaging_throws()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Messaging error"));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.JoinUserChannelAsync("channelId");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Messaging error*");
    }

    [Fact]
    public async Task CreateAppChannelAsync_returns_channel_when_successful()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new CreateAppChannelResponse
        {
            Success = true,
            Error = null
        };

        messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var result = await factory.CreateAppChannelAsync("channelId");

        result.Id.Should().Be("channelId");
    }

    [Fact]
    public async Task CreateAppChannelAsync_returns_error_when_response_is_null()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?) null));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.CreateAppChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task CreateAppChannelAsync_returns_error_when_response_has_error()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new CreateAppChannelResponse
        {
            Success = false,
            Error = "Some error"
        };

        messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.CreateAppChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task CreateAppChannelAsync_returns_error_when_response_is_unsuccessful()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new CreateAppChannelResponse
        {
            Success = false,
            Error = null
        };

        messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.CreateAppChannelAsync("channelId");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*UnspecifiedReason*");
    }

    [Fact]
    public async Task CreateAppChannelAsync_returns_error_when_messaging_throws()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Messaging error"));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.CreateAppChannelAsync("channelId");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Messaging error*");
    }

    [Fact]
    public async Task FindChannelAsync_returns_Channel_when_found()
    {
        var response = new FindChannelResponse
        {
            Found = true
        };

        var responseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);

        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindChannel,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);
        var result = await factory.FindChannelAsync("myChannel", ChannelType.User);

        result.Id.Should().Be("myChannel");
        result.Type.Should().Be(ChannelType.User);
    }

    [Fact]
    public async Task FindChannelAsync_throws_when_response_is_null()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindChannel,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?) null);

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);
        var act = async () => await factory.FindChannelAsync("myChannel", ChannelType.User);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received*");
    }

    [Fact]
    public async Task FindChannelAsync_throws_when_response_has_error()
    {
        var response = new FindChannelResponse
        {
            Error = "Some error"
        };

        var responseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindChannel,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);
        var act = async () => await factory.FindChannelAsync("myChannel", ChannelType.User);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindChannelAsync_throws_when_channel_not_found()
    {
        var response = new FindChannelResponse
        {
            Found = false
        };

        var responseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindChannel,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);
        var act = async () => await factory.FindChannelAsync("myChannel", ChannelType.User);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task FindChannelAsync_joins_private_channel()
    {
        var messagingMock = new Mock<IMessaging>();
        var channelId = "private-channel";

        var findChannelResponse = new FindChannelResponse { Found = true };
        var joinPrivateChannelResponse = new JoinPrivateChannelResponse { Success = true };

        messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(findChannelResponse, _jsonSerializerOptions))
            .ReturnsAsync(JsonSerializer.Serialize(joinPrivateChannelResponse, _jsonSerializerOptions));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var result = await factory.FindChannelAsync(channelId, ChannelType.Private);

        result.Should().BeAssignableTo<IPrivateChannel>();
        result.Id.Should().Be(channelId);
    }

    [Fact]
    public async Task FindChannelAsync_try_to_join_private_channel_but_throws_when_missing_response()
    {
        var messagingMock = new Mock<IMessaging>();
        var instanceId = "test-instance";
        var channelId = "private-channel";

        var findChannelResponse = new FindChannelResponse { Found = true };

        messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(findChannelResponse, _jsonSerializerOptions))
            .ReturnsAsync(JsonSerializer.Serialize((string?)null));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async() => await factory.FindChannelAsync(channelId, ChannelType.Private);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response*");
    }

    [Fact]
    public async Task FindChannelAsync_try_to_join_private_channel_but_throws_when_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var instanceId = "test-instance";
        var channelId = "private-channel";

        var findChannelResponse = new FindChannelResponse { Found = true };
        var joinPrivateChannelResponse = new JoinPrivateChannelResponse { Success = true, Error = "Some error" };

        messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(findChannelResponse, _jsonSerializerOptions))
            .ReturnsAsync(JsonSerializer.Serialize(joinPrivateChannelResponse, _jsonSerializerOptions));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.FindChannelAsync(channelId, ChannelType.Private);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindChannelAsync_try_to_join_private_channel_but_throws_when_no_success_response_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var instanceId = "test-instance";
        var channelId = "private-channel";

        var findChannelResponse = new FindChannelResponse { Found = true };
        var joinPrivateChannelResponse = new JoinPrivateChannelResponse { Success = false };

        messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(findChannelResponse, _jsonSerializerOptions))
            .ReturnsAsync(JsonSerializer.Serialize(joinPrivateChannelResponse, _jsonSerializerOptions));

        var desktopAgentClientMock = new Mock<IDesktopAgent>();
        var factory = new ChannelHandler(messagingMock.Object, "instanceId", desktopAgentClientMock.Object);

        var act = async () => await factory.FindChannelAsync(channelId, ChannelType.Private);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Client was not able to join to private channel*");
    }
}
