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

using System.Reflection;
using System.Text.Json;
using Finos.Fdc3;
using Finos.Fdc3.Context;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using ContextMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ContextMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure;

public class DesktopAgentClientFdc3_3_0Tests
{
    private static readonly JsonSerializerOptions JsonOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    [Fact]
    public async Task AddIntentListenerWithContext_filters_non_matching_context_type()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();

        var intentsClient = new Mock<IIntentsClient>();
        IntentHandler<Instrument>? capturedHandler = null;
        var listenerMock = new Mock<IListener>();
        intentsClient
            .Setup(m => m.AddIntentListenerAsync("test-intent-filter", It.IsAny<IntentHandler<Instrument>>()))
            .Callback<string, IntentHandler<Instrument>>((_, handler) => capturedHandler = handler)
            .ReturnsAsync(listenerMock.Object);

        SetPrivateField(desktopAgent, "_intentsClient", intentsClient.Object);

        var invocationCount = 0;
        await desktopAgent.AddIntentListenerWithContext<Instrument>(
            "test-intent-filter",
            "fdc3.contact",
            (context, metadata) =>
            {
                invocationCount++;
                return Task.FromResult<IIntentResult>(new Instrument());
            });

        await capturedHandler!(new Instrument(), null);

        invocationCount.Should().Be(0);
    }

    [Fact]
    public async Task AddIntentListenerWithContext_invokes_handler_for_matching_context_type()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();

        var intentsClient = new Mock<IIntentsClient>();
        IntentHandler<Instrument>? capturedHandler = null;
        intentsClient
            .Setup(m => m.AddIntentListenerAsync("test-intent-filter", It.IsAny<IntentHandler<Instrument>>()))
            .Callback<string, IntentHandler<Instrument>>((_, handler) => capturedHandler = handler)
            .ReturnsAsync(Mock.Of<IListener>());

        SetPrivateField(desktopAgent, "_intentsClient", intentsClient.Object);

        var invocationCount = 0;
        await desktopAgent.AddIntentListenerWithContext<Instrument>(
            "test-intent-filter",
            "fdc3.instrument",
            (context, metadata) =>
            {
                invocationCount++;
                return Task.FromResult<IIntentResult>(new Instrument());
            });

        await capturedHandler!(new Instrument(), null);

        invocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Open_with_metadata_overload_returns_result()
    {
        var messagingMock = new Mock<IMessaging>();
        var openResponse = new OpenResponse
        {
            AppIdentifier = new AppIdentifier { AppId = "target-app", InstanceId = "target-instance" }
        };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(openResponse, JsonOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object, "test-app", "test-instance");
        var result = await desktopAgent.Open(
            new AppIdentifier { AppId = "target-app" },
            new Instrument(),
            new ContextMetadata { TraceId = "trace-open" });

        result.AppId.Should().Be("target-app");
    }

    [Fact]
    public async Task RaiseIntent_with_new_instance_and_metadata_overload_returns_resolution()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();

        var metadata = new ContextMetadata { TraceId = "trace-intent" };
        var resolution = new Mock<IIntentResolution>();
        var intentsClient = new Mock<IIntentsClient>();
        intentsClient
            .Setup(m => m.RaiseIntentAsync("ViewChart", It.IsAny<Instrument>(), null, true, metadata))
            .ReturnsAsync(resolution.Object);
        SetPrivateField(desktopAgent, "_intentsClient", intentsClient.Object);

        var result = await desktopAgent.RaiseIntent(
            "ViewChart",
            new Instrument(),
            app: null,
            newInstance: true,
            metadata: metadata);

        result.Should().Be(resolution.Object);
    }

    [Fact]
    public async Task RaiseIntentForContext_with_new_instance_and_metadata_overload_returns_resolution()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();

        var metadata = new ContextMetadata { TraceId = "trace-intent-context" };
        var resolution = new Mock<IIntentResolution>();
        var intentsClient = new Mock<IIntentsClient>();
        intentsClient
            .Setup(m => m.RaiseIntentForContextAsync(It.IsAny<Instrument>(), null, true, metadata))
            .ReturnsAsync(resolution.Object);
        SetPrivateField(desktopAgent, "_intentsClient", intentsClient.Object);

        var result = await desktopAgent.RaiseIntentForContext(
            new Instrument(),
            app: null,
            newInstance: true,
            metadata: metadata);

        result.Should().Be(resolution.Object);
    }

    [Fact]
    public async Task AddEventListener_returns_listener_when_joined_to_channel()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true }, JsonOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object, "test-app", "test-instance");
        await desktopAgent.JoinUserChannel("green");

        var listener = await desktopAgent.AddEventListener("context-changed", _ => { });

        listener.Should().NotBeNull();
    }

    [Fact]
    public async Task AddEventListener_throws_when_not_joined_to_channel()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();

        var act = async () => await desktopAgent.AddEventListener("context-changed", _ => { });

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*No current channel*");
    }

    [Fact]
    public async Task Close_leaves_current_channel()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true }, JsonOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object, "test-app", "test-instance");
        await desktopAgent.JoinUserChannel("green");

        await desktopAgent.Close();
        var currentChannel = await desktopAgent.GetCurrentChannel();

        currentChannel.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_disposes_channel_handler_when_present()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();
        var channelHandler = new Mock<IChannelHandler>();
        channelHandler
            .Setup(h => h.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        SetPrivateField(desktopAgent, "_channelHandler", channelHandler.Object);

        await desktopAgent.DisposeAsync();

        channelHandler.Verify(h => h.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_unsubscribes_intent_and_context_listeners_when_handler_missing()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();
        SetPrivateField(desktopAgent, "_channelHandler", null!);

        var intentListener = new Mock<IListener>();
        intentListener.Setup(l => l.Unsubscribe()).Returns(Task.CompletedTask);
        var contextListener = new Mock<IListener>();
        contextListener.Setup(l => l.Unsubscribe()).Returns(Task.CompletedTask);

        var intentListenersField = typeof(DesktopAgentClient).GetField("_intentListeners", BindingFlags.Instance | BindingFlags.NonPublic);
        var contextListenersField = typeof(DesktopAgentClient).GetField("_contextListeners", BindingFlags.Instance | BindingFlags.NonPublic);
        intentListenersField.Should().NotBeNull();
        contextListenersField.Should().NotBeNull();

        var intentListeners = (System.Collections.Concurrent.ConcurrentDictionary<string, IListener>)intentListenersField!.GetValue(desktopAgent)!;
        var contextListeners = (Dictionary<IListener, Func<string, ChannelType, CancellationToken, ValueTask>>)contextListenersField!.GetValue(desktopAgent)!;

        intentListeners.TryAdd("intent", intentListener.Object);
        contextListeners.Add(contextListener.Object, (_, _, _) => ValueTask.CompletedTask);

        await desktopAgent.DisposeAsync();

        intentListener.Verify(l => l.Unsubscribe(), Times.Once);
        contextListener.Verify(l => l.Unsubscribe(), Times.Once);
    }

    [Fact]
    public async Task GetOpenedAppContextAsync_handles_fdc3_exception_from_open_client()
    {
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger>();
        loggerMock.Setup(l => l.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error)).Returns(true);
        var loggerFactoryMock = new Mock<Microsoft.Extensions.Logging.ILoggerFactory>();
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.RegisterServiceAsync(It.IsAny<string>(), It.IsAny<ServiceHandler>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var desktopAgent = new DesktopAgentClient(messagingMock.Object, "test-app", "test-instance", loggerFactory: loggerFactoryMock.Object);
        var openClient = new Mock<IOpenClient>();
        openClient
            .Setup(c => c.GetOpenAppContextAsync("ctx-id"))
            .ThrowsAsync(new Fdc3DesktopAgentException("open-failed"));

        SetPrivateField(desktopAgent, "_openClient", openClient.Object);

        var method = typeof(DesktopAgentClient).GetMethod("GetOpenedAppContextAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        var task = (ValueTask)method!.Invoke(desktopAgent, new object[] { "ctx-id" })!;

        Func<Task> act = async () => await task.AsTask();
        await act.Should().NotThrowAsync();
        loggerMock.Verify(l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Error,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinUserChannel_leaves_previous_channel_and_unsubscribes_context_listeners()
    {
        var desktopAgent = CreateClientWithMinimalMessaging();
        await desktopAgent.GetCurrentChannel();

        var oldChannel = new Mock<IChannel>();
        oldChannel.SetupGet(c => c.Id).Returns("old");
        oldChannel.SetupGet(c => c.Type).Returns(ChannelType.User);

        var newChannel = new Mock<IChannel>();
        newChannel.SetupGet(c => c.Id).Returns("new");
        newChannel.SetupGet(c => c.Type).Returns(ChannelType.User);

        var channelHandler = new Mock<IChannelHandler>();
        channelHandler
            .Setup(h => h.JoinUserChannelAsync("new"))
            .Returns(new ValueTask<IChannel>(newChannel.Object));

        var contextListener = new Mock<IListener>();
        contextListener.Setup(l => l.Unsubscribe()).Returns(Task.CompletedTask);

        SetPrivateField(desktopAgent, "_currentChannel", oldChannel.Object);
        SetPrivateField(desktopAgent, "_channelHandler", channelHandler.Object);

        var contextListenersField = typeof(DesktopAgentClient).GetField("_contextListeners", BindingFlags.Instance | BindingFlags.NonPublic);
        contextListenersField.Should().NotBeNull();
        var contextListeners = (Dictionary<IListener, Func<string, ChannelType, CancellationToken, ValueTask>>)contextListenersField!.GetValue(desktopAgent)!;
        contextListeners.Add(contextListener.Object, (_, _, _) => ValueTask.CompletedTask);

        await desktopAgent.JoinUserChannel("new");

        contextListener.Verify(l => l.Unsubscribe(), Times.Once);
        channelHandler.Verify(h => h.JoinUserChannelAsync("new"), Times.Once);
    }

    private static DesktopAgentClient CreateClientWithMinimalMessaging()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.RegisterServiceAsync(It.IsAny<string>(), It.IsAny<ServiceHandler>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        return new DesktopAgentClient(messagingMock.Object, "test-app", "test-instance");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull($"Field '{fieldName}' should exist for test setup.");
        field!.SetValue(target, value);
    }
}
