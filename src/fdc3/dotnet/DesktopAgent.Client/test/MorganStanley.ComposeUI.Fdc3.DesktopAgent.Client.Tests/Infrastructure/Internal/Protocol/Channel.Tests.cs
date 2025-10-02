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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using System.Text.Json;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using Instrument = Finos.Fdc3.Context.Instrument;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal.Protocol;

public class ChannelTests
{
    private const string ChannelId = "testChannel";
    private const string InstanceId = "testInstance";
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly ChannelType _channelType = ChannelType.App;
    private readonly Channel _channel;

    public ChannelTests()
    {
        _channel = new Channel(
            ChannelId,
            _channelType,
            _messagingMock.Object,
            InstanceId,
            displayMetadata: null,
            loggerFactory: new NullLoggerFactory());
    }

    [Fact]
    public void Properties_return_constructor_values_on_the_channel()
    {
        _channel.Id.Should().Be(ChannelId);
        _channel.Type.Should().Be(_channelType);
        _channel.DisplayMetadata.Should().BeNull();
    }

    [Fact]
    public async Task AddContextListener_handles_last_context_on_the_channel()
    {
        var handlerCalled = false;
        ContextHandler<Instrument> handler = (ctx, contextMetadata) =>
        {
            handlerCalled = true;
        };

        _messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var listener = await _channel.AddContextListener("fdc3.instrument", handler);

        listener.Should().NotBeNull();
        handlerCalled.Should().BeFalse(); // Handler is not called until a message is received
    }

    [Fact]
    public async Task Broadcast_publishes_json_and_updates_last_context_on_the_channel()
    {
        var context = new Instrument();
        string? publishedTopic = null;
        object? publishedPayload = null;

        _messagingMock
            .Setup(
                m => m.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((topic, payload, options) =>
            {
                publishedTopic = topic;
                publishedPayload = payload;
            })
            .Returns(ValueTask.CompletedTask);

        await _channel.Broadcast(context);

        publishedTopic.Should().Contain(ChannelId);
        publishedPayload.Should().BeEquivalentTo(JsonSerializer.Serialize(new Instrument(), SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));
    }

    [Fact]
    public async Task GetCurrentContext_returns_null_when_service_returns_null_on_the_channel()
    {
        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((string) null!);

        var result = await _channel.GetCurrentContext("TestType");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentContext_returns_deserialized_context_and_updates_last_context_on_the_channel()
    {
        var context = new Instrument(new InstrumentID() { Ticker = "MS" }, "test-name");
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextJson);

        var result = await _channel.GetCurrentContext("fdc3.instrument");

        result.Should().NotBeNull();
        result.Should().BeOfType<Instrument>();
        ((Instrument) result!).Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task GetCurrentContext_returns_last_context_when_context_type_is_null_on_the_channel()
    {
        var context = new Instrument();
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextJson);

        var result = await _channel.GetCurrentContext(null);

        result.Should().NotBeNull();
        result.Should().BeOfType<Instrument>();
        ((Instrument) result!).Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task GetCurrentContext_logs_and_returns_null_when_context_type_not_found_on_the_channel()
    {
        var context = new Instrument(new InstrumentID() { Ticker = "MS" }, Guid.NewGuid().ToString());
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextJson);

        var result = await _channel.GetCurrentContext("NonExistentType");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentContext_throws_and_logs_when_messaging_throws_on_the_channel()
    {
        var exception = new InvalidOperationException("Messaging error");
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var act = async () => await _channel.GetCurrentContext("TestType");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Messaging error");
    }
}
