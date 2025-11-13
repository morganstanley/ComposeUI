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

using Finos.Fdc3;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using System.Text.Json;
using IntentResolution = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal.Protocol.IntentResolution;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal.Protocol;

public class IntentResolutionTests
{
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly Mock<IChannelFactory> _channelFactoryMock = new();
    private readonly Mock<ILogger<IntentResolution>> _loggerMock = new();
    private readonly string _messageId = "msg-1";
    private readonly string _intent = "ViewChart";
    private readonly AppIdentifier _source = new() { AppId = "app", InstanceId = "inst" };
    private readonly JsonSerializerOptions _jsonOptions = new();

    private IntentResolution CreateIntentResolution()
    {
        return new IntentResolution(
            _messageId,
            _messagingMock.Object,
            _channelFactoryMock.Object,
            _intent,
            _source,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetResult_returns_channel_when_channelId_and_type_are_present()
    {
        var response = new GetIntentResultResponse
        {
            ChannelId = "ch1",
            ChannelType = ChannelType.User
        };

        var channelMock = new Mock<IChannel>();

        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonOptions));

        _channelFactoryMock
            .Setup(f => f.FindChannelAsync("ch1", ChannelType.User))
            .ReturnsAsync(channelMock.Object);

        var intentResolution = CreateIntentResolution();
        var result = await intentResolution.GetResult();

        result.Should().Be(channelMock.Object);
    }

    [Fact]
    public async Task GetResult_returns_context_when_context_is_present()
    {
        var context = new Instrument(new InstrumentID { Ticker = "AAPL" });
        var response = new GetIntentResultResponse
        {
            Context = JsonSerializer.Serialize(context, _jsonOptions)
        };
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonOptions));

        var intentResolution = CreateIntentResolution();
        var result = await intentResolution.GetResult();

        result.Should().BeOfType<Instrument>();
        ((Instrument) result!).ID!.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetResult_returns_null_when_voidResult_is_true()
    {
        var response = new GetIntentResultResponse
        {
            VoidResult = true
        };
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonOptions));

        var intentResolution = CreateIntentResolution();
        var result = await intentResolution.GetResult();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetResult_throws_when_response_is_null()
    {
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?) null);

        var intentResolution = CreateIntentResolution();
        var act = async () => await intentResolution.GetResult();

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received*");
    }

    [Fact]
    public async Task GetResult_throws_when_response_has_error()
    {
        var response = new GetIntentResultResponse
        {
            Error = "Some error"
        };
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonOptions));

        var intentResolution = CreateIntentResolution();
        var act = async () => await intentResolution.GetResult();

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task GetResult_throws_when_no_valid_result()
    {
        var response = new GetIntentResultResponse();
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.GetIntentResult,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonOptions));

        var intentResolution = CreateIntentResolution();
        var act = async () => await intentResolution.GetResult();

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{_intent}*");
    }
}
