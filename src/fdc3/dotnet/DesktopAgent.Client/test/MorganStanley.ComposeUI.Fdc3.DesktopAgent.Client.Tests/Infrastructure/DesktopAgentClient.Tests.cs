﻿/*
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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ImplementationMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure;

public class DesktopAgentClientTests : IAsyncLifetime
{
    [Fact]
    public async Task GetAppMetadata_returns_null_response()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task GetAppMetadata_returns_error_response()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetAppMetadataResponse { Error = "test-error" }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test-error*");
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppIdentifier()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetAppMetadataResponse { AppMetadata = new AppMetadata { AppId = "test-appId" } }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result =  await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new AppMetadata {AppId = "test-appId"});
    }

    [Fact]
    public async Task GetInfo_throws_error_as_no_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetInfo();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task GetInfo_throws_error_as_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetInfoResponse { Error = "test-error" }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetInfo();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test-error*");
    }

    [Fact]
    public async Task GetInfo_returns_ImplementationMetadata()
    {
        var messagingMock = new Mock<IMessaging>();
        var implementationMetadata = new ImplementationMetadata {AppMetadata = new AppMetadata {AppId = "test_appId"}};

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetInfoResponse { ImplementationMetadata = implementationMetadata }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.GetInfo();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(implementationMetadata);
    }

    [Fact]
    public async Task JoinUserChannel_joins_to_a_channel()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        await desktopAgent.JoinUserChannel("test-channelId");

        var channel = await desktopAgent.GetCurrentChannel();

        channel.Should().NotBeNull();
        channel!.Id.Should().Be("test-channelId");
    }

    [Fact]
    public async Task JoinUserChannel_returns_error()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = false, Error = "test-error" }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var action = async() => await desktopAgent.JoinUserChannel("test-channelId");

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test-error*");
    }

    [Fact]
    public async Task JoinUserChannel_returns_error_when_not_successful()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = false, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var action = async () => await desktopAgent.JoinUserChannel("test-channelId");

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*app was not able to join to user channel*");
    }

    [Fact]
    public async Task JoinUserChannel_handles_last_context_for_all_the_registered_top_level_context_listener()
    {
        var resultContextListenerInvocations = 0;
        var subscriptionMock = new Mock<IAsyncDisposable>();

        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
            _ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IAsyncDisposable>(subscriptionMock.Object));

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name"), _jsonSerializerOptions))) //GetCurrentContext response after joining to the channels when iterating through the context listeners
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Context("test-type"), _jsonSerializerOptions))); //This shouldn't be the last context which is received by the second context listener as the last context for the registered context type is already set when invoking the GetCurrentContext for the first context listener

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var listener = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) =>  { resultContextListenerInvocations++; });
        var listener2 = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });

        await desktopAgent.JoinUserChannel("test-channelId");

        var channel = await desktopAgent.GetCurrentChannel();

        channel.Should().NotBeNull();
        channel!.Id.Should().Be("test-channelId");
        resultContextListenerInvocations.Should().Be(2);
    }

    [Fact]
    public async Task LeaveCurrentChannel_leaves_the_currently_used_channel()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions))); 

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        await desktopAgent.JoinUserChannel("test-channelId");

        var channel = await desktopAgent.GetCurrentChannel();

        channel.Should().NotBeNull();
        channel!.Id.Should().Be("test-channelId");

        await desktopAgent.LeaveCurrentChannel();
        channel = await desktopAgent.GetCurrentChannel();
        channel.Should().BeNull();
    }

    [Fact]
    public async Task AddContextListener_registers_top_level_listener_but_not_receiving_messages_until_its_joined_to_a_channel()
    {
        var resultContextListenerInvocations = 0;
        var subscriptionMock = new Mock<IAsyncDisposable>();

        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
            _ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IAsyncDisposable>(subscriptionMock.Object));

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var listener = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });
        var listener2 = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });

        resultContextListenerInvocations.Should().Be(0);
    }

    [Fact]
    public async Task AddContextListener_handles_last_context_on_the_channel()
    {
        var resultContextListenerInvocations = 0;
        var subscriptionMock = new Mock<IAsyncDisposable>();

        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
            _ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<IAsyncDisposable>(subscriptionMock.Object));

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name"), _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Context("test-type"), _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        await desktopAgent.JoinUserChannel("test-channelId");

        var listener = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });
        var listener2 = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });

        resultContextListenerInvocations.Should().Be(2);
    }

    [Fact]
    public async Task AddContextListener_receives_messages()
    {
        var resultContextListenerInvocations = 0;
        var subscriptionMock = new Mock<IAsyncDisposable>();
        List<TopicMessageHandler> capturedHandlers = new();

        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
            _ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns((string topic, TopicMessageHandler handler, CancellationToken ct) =>
            {
                capturedHandlers.Add(handler);
                return new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>());
            });

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name"), _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new AddContextListenerResponse { Success = true, Id = Guid.NewGuid().ToString() }, _jsonSerializerOptions)))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new Context("test-type"), _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        await desktopAgent.JoinUserChannel("test-channelId");

        var listener = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });
        var listener2 = await desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContextListenerInvocations++; });

        foreach (var capturedHandler in capturedHandlers)
        {
            //Instead of broadcasting the messages we are triggering the handlers directly
            await capturedHandler!(JsonSerializer.Serialize(new Instrument(new InstrumentID { Ticker = Guid.NewGuid().ToString() }, Guid.NewGuid().ToString()), _jsonSerializerOptions));
        }

        resultContextListenerInvocations.Should().Be(4);
    }

    [Fact]
    public async Task Broadcast_throws_error_on_no_current_channel()
    {
        var messagingMock = new Mock<IMessaging>();

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.Broadcast(new Context("test-type"));

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No current channel to broadcast the context to.*");
    }

    [Fact]
    public async Task Broadcast_sends_message()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
            _ => _.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        messagingMock.SetupSequence(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new JoinUserChannelResponse { Success = true, DisplayMetadata = new DisplayMetadata() { Name = "test-channelId" } }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        await desktopAgent.JoinUserChannel("test-channelId");

        await desktopAgent.Broadcast(new Instrument(new InstrumentID { Ticker = "test-instrument-broadcasted" }, "test-name-broadcasted"));

        messagingMock.Verify(
            _ => _.PublishAsync(
                It.Is<string>(topic => topic == new ChannelTopics("test-channelId", ChannelType.User).Broadcast),
                It.Is<string>(contextJson => contextJson.Contains("test-instrument-broadcasted")),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "test-appId2");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), Guid.NewGuid().ToString());

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;
}