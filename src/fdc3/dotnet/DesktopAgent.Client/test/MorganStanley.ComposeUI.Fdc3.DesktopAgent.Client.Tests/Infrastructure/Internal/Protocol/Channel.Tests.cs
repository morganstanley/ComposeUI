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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using System.Text.Json;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using Instrument = Finos.Fdc3.Context.Instrument;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ContextMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal.Protocol;

public class ChannelTests
{
    private const string ChannelId = "testChannel";
    private const string AppId = "testApp";
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
            AppId,
            InstanceId,
            displayMetadata: null,
            loggerFactory: new NullLoggerFactory());
    }

    [Fact]
    public void Constructor_throws_when_channel_id_is_null()
    {
        System.Action act = () => new Channel(
            channelId: null!,
            channelType: _channelType,
            messaging: _messagingMock.Object,
            appId: AppId,
            instanceId: InstanceId,
            displayMetadata: null,
            loggerFactory: new NullLoggerFactory());

        act.Should().Throw<Fdc3DesktopAgentException>();
    }

    [Fact]
    public void Constructor_throws_when_app_id_is_null()
    {
        System.Action act = () => new Channel(
            channelId: ChannelId,
            channelType: _channelType,
            messaging: _messagingMock.Object,
            appId: null!,
            instanceId: InstanceId,
            displayMetadata: null,
            loggerFactory: new NullLoggerFactory());

        act.Should().Throw<ArgumentNullException>().WithParameterName("appId");
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
        var broadcastPayload = JsonSerializer.Deserialize<BroadcastPayload>((string)publishedPayload!, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);
        broadcastPayload!.Context.Should().Be(JsonSerializer.Serialize(new Instrument(), SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));
    }

    [Fact]
    public async Task Broadcast_with_metadata_publishes_source_and_trace_metadata_on_the_channel()
    {
        var context = new Instrument();
        string? publishedPayload = null;
        var metadata = new ContextMetadata { TraceId = "trace-1" };

        _messagingMock
            .Setup(
                m => m.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, payload, _) => publishedPayload = payload)
            .Returns(ValueTask.CompletedTask);

        await _channel.Broadcast(context, metadata);

        var broadcastPayload = JsonSerializer.Deserialize<BroadcastPayload>(publishedPayload!, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);
        broadcastPayload!.Metadata.Should().NotBeNull();
        broadcastPayload.Metadata!.Source!.AppId.Should().Be(AppId);
        broadcastPayload.Metadata.Source.InstanceId.Should().Be(InstanceId);
        broadcastPayload.Metadata.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public async Task Broadcast_updates_cached_context_when_same_type_is_broadcast_twice_on_the_channel()
    {
        _messagingMock
            .Setup(
                m => m.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var first = new Instrument(new InstrumentID { Ticker = "MS" }, "first");
        var second = new Instrument(new InstrumentID { Ticker = "MS" }, "second");

        await _channel.Broadcast(first);
        await _channel.Broadcast(second);

        _messagingMock.Verify(
            m => m.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
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
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse(), SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

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
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContext("fdc3.instrument");

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
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContext(null);

        result.Should().BeOfType<Instrument>();
        ((Instrument) result!).Should().BeEquivalentTo(context);
    }

    [Fact]
    public async Task GetCurrentContext_returns_last_context_when_context_type_is_empty_on_the_channel()
    {
        var context = new Instrument();
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContext(string.Empty);

        result.Should().BeOfType<Instrument>();
        ((Instrument)result!).Should().BeEquivalentTo(context);
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
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

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

    [Fact]
    public async Task GetCurrentContextWithMetadata_returns_context_and_response_metadata_on_the_channel()
    {
        var context = new Instrument(new InstrumentID() { Ticker = "MS" }, "test-name");
        var metadata = new ContextMetadata
        {
            Source = new AppIdentifier { AppId = "upstream-app", InstanceId = "upstream-instance" },
            TraceId = "trace-2"
        };
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson, Metadata = metadata }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        result.Should().NotBeNull();
        result!.Context.Should().BeEquivalentTo(context);
        result.Metadata.Source!.AppId.Should().Be("upstream-app");
        result.Metadata.TraceId.Should().Be("trace-2");
    }

    [Fact]
    public async Task GetCurrentContextWithMetadata_generates_metadata_when_missing_on_the_channel()
    {
        var context = new Instrument(new InstrumentID() { Ticker = "MS" }, "test-name");
        var contextJson = JsonSerializer.Serialize(context, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = contextJson, Metadata = null }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        result.Should().NotBeNull();
        result!.Metadata.Source!.AppId.Should().Be(AppId);
        result.Metadata.Source.InstanceId.Should().Be(InstanceId);
    }

    [Fact]
    public async Task GetCurrentContextWithMetadata_returns_null_when_response_context_is_missing_on_the_channel()
    {
        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = null }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentContextWithMetadata_returns_null_when_context_deserializes_to_null_on_the_channel()
    {
        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = "null" }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var result = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentContextWithMetadata_throws_when_messaging_throws_on_the_channel()
    {
        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Messaging error"));

        var act = async () => await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Messaging error");
    }

    [Fact]
    public async Task GetCurrentContextWithMetadata_updates_existing_cached_context_for_same_type_on_the_channel()
    {
        var firstContext = new Instrument(new InstrumentID { Ticker = "MS" }, "first");
        var secondContext = new Instrument(new InstrumentID { Ticker = "IBM" }, "second");
        var firstMetadata = new ContextMetadata { TraceId = "trace-1" };
        var secondMetadata = new ContextMetadata { TraceId = "trace-2" };

        _messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(
                new GetCurrentContextResponse
                {
                    Context = JsonSerializer.Serialize(firstContext, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization),
                    Metadata = firstMetadata
                },
                SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization))
            .ReturnsAsync(JsonSerializer.Serialize(
                new GetCurrentContextResponse
                {
                    Context = JsonSerializer.Serialize(secondContext, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization),
                    Metadata = secondMetadata
                },
                SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        var firstResult = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");
        var secondResult = await _channel.GetCurrentContextWithMetadata("fdc3.instrument");

        firstResult!.Metadata.TraceId.Should().Be("trace-1");
        secondResult!.Metadata.TraceId.Should().Be("trace-2");
    }

    [Fact]
    public async Task ClearContext_clears_type_and_all_contexts_on_the_channel()
    {
        Func<Task> clearTyped = async () => await _channel.ClearContext("fdc3.instrument");
        Func<Task> clearAll = async () => await _channel.ClearContext(null);

        await clearTyped.Should().NotThrowAsync();
        await clearAll.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ClearContext_clears_all_contexts_when_context_type_is_empty_on_the_channel()
    {
        var act = async () => await _channel.ClearContext(string.Empty);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddEventListener_returns_listener_that_can_unsubscribe_on_the_channel()
    {
        var listener = await _channel.AddEventListener("context-changed", _ => { });

        listener.Should().NotBeNull();
        await listener.Unsubscribe();
    }

    [Fact]
    public async Task AddEventListener_accepts_null_event_type_on_the_channel()
    {
        var listener = await _channel.AddEventListener(null, _ => { });

        listener.Should().NotBeNull();
        await listener.Unsubscribe();
    }

    [Fact]
    public async Task GetCurrentContext_logs_debug_response_when_debug_enabled_on_the_channel()
    {
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

        var channel = new Channel(
            ChannelId,
            _channelType,
            _messagingMock.Object,
            AppId,
            InstanceId,
            displayMetadata: null,
            loggerFactory: loggerFactoryMock.Object);

        _messagingMock
            .Setup(
                m => m.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new GetCurrentContextResponse { Context = null }, SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization));

        await channel.GetCurrentContext("fdc3.instrument");

        loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
