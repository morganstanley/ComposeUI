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

using System.Net.Http.Headers;
using System.Text.Json;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class OpenClientTests
{
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly Mock<IChannelHandler> _channelHandlerMock = new();
    private readonly Mock<IListener> _listenerMock = new();
    private readonly Mock<IDesktopAgent> _desktopAgentMock = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    [Fact]
    public async Task GetOpenedAppContextAsync_returns_context()
    {
        var response = new GetOpenedAppContextResponse
        {
            Context = JsonSerializer.Serialize(new Instrument(), _jsonSerializerOptions)
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var context = await openClient.GetOpenAppContextAsync(Guid.NewGuid().ToString());

        context.Type.Should().Be("fdc3.instrument");
    }

    [Fact]
    public async Task GetOpenedAppContextAsync_throws_when_null_response_received()
    {
        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async() => await openClient.GetOpenAppContextAsync(Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response*");
    }

    [Fact]
    public async Task GetOpenedAppContextAsync_throws_when_null_context_id_passed()
    {
        var response = new GetOpenedAppContextResponse
        {
            Context = JsonSerializer.Serialize(new Instrument(), _jsonSerializerOptions)
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.GetOpenAppContextAsync(null!);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*The context id was received*");
    }

    [Fact]
    public async Task GetOpenedAppContextAsync_throws_when_error_response_received()
    {
        var response = new GetOpenedAppContextResponse
        {
            Error = "Some error"
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.GetOpenAppContextAsync(Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task GetOpenedAppContextAsync_throws_when_null_context_returned()
    {
        var response = new GetOpenedAppContextResponse
        {
            Context = null
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.GetOpenAppContextAsync(Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No context was received*");
    }

    [Fact]
    public async Task OpenAsync_returns_AppIdentifier()
    {
        var response = new OpenResponse
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId",
                InstanceId = "instanceId"
            }
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        _desktopAgentMock.Setup(
            _ => _.GetCurrentChannel())
            .ReturnsAsync((IChannel?) null);

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var result = await openClient.OpenAsync(new AppIdentifier { AppId = "appId" }, new Instrument());

        result.AppId.Should().Be("appId");
        result.InstanceId.Should().Be("instanceId");
    }

    [Fact]
    public async Task OpenAsync_throws_when_malformed_context()
    {
        var openClient = new OpenClient(Guid.NewGuid().ToString(), _messagingMock.Object, _desktopAgentMock.Object);

        var act = async () => await openClient.OpenAsync(new AppIdentifier { AppId = "appId" }, new MyUnvalidContext());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*context is malformed*");
    }

    [Fact]
    public async Task OpenAsync_throws_when_null_response_received()
    {
        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.OpenAsync(new AppIdentifier { AppId = "appId" }, new Instrument());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response*");
    }

    [Fact]
    public async Task OpenAsync_throws_when_error_response_received()
    {
        var response = new OpenResponse
        {
            Error = "Some error"
        };

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        _desktopAgentMock.Setup(
            _ => _.GetCurrentChannel())
            .ReturnsAsync((IChannel?) null);

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.OpenAsync(new AppIdentifier { AppId = "appId" }, new Instrument());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task OpenAsync_throws_when_AppIdentifier_not_received()
    {
        var response = new OpenResponse();

        _messagingMock
            .Setup(_ => _.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        _desktopAgentMock.Setup(
            _ => _.GetCurrentChannel())
            .ReturnsAsync((IChannel?) null);

        var openClient = new OpenClient(
            Guid.NewGuid().ToString(),
            _messagingMock.Object,
            _desktopAgentMock.Object);

        var act = async () => await openClient.OpenAsync(new AppIdentifier { AppId = "appId" }, new Instrument());

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*AppIdentifier cannot be returned*");

    }

    internal class MyUnvalidContext : IContext
    {
        public object? ID => new object();

        public string? Name => "";

        public string Type => "";

        public dynamic? Native { get; set; }
    }
}
