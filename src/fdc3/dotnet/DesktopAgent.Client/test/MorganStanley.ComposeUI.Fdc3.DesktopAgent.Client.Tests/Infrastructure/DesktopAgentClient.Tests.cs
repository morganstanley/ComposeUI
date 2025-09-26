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
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ImplementationMetadata;
using IntentMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.IntentMetadata;

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

    [Fact]
    public async Task GetUserChannels_returns_channels()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(
                JsonSerializer.Serialize(
                    new GetUserChannelsResponse 
                    { 
                        Channels = new[] 
                        { 
                            new ChannelItem { Id = "1", DisplayMetadata = new DisplayMetadata { Name = "1" } }, 
                            new ChannelItem { Id = "2", DisplayMetadata = new DisplayMetadata { Name = "2" } } 
                        } 
                    }, _jsonSerializerOptions )));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.GetUserChannels();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(
            new[] 
            { 
                new Channel("1", ChannelType.User, messagingMock.Object, It.IsAny<string>(), new DisplayMetadata { Name = "1" }, It.IsAny<ILoggerFactory>()), 
                new Channel("2", ChannelType.User, messagingMock.Object, It.IsAny<string>(), new DisplayMetadata { Name = "2" }, It.IsAny<ILoggerFactory>()) 
            });
    }

    [Fact]
    public async Task GetUserChannels_throws_error_on_no_response()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?)null));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetUserChannels();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task GetUserChannels_throws_error_on_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(
                JsonSerializer.Serialize(
                    new GetUserChannelsResponse
                    {
                        Channels = null
                    }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetUserChannels();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*The DesktopAgent backend did not return any channel.*");
    }

    [Fact]
    public async Task GetUserChannels_does_ot_include_channel_if_displaymetadata_is_missing()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(
                JsonSerializer.Serialize(
                    new GetUserChannelsResponse
                    {
                        Channels = new[]
                        {
                            new ChannelItem { Id = "test-id1", DisplayMetadata = new DisplayMetadata { Name = "1" } },
                            new ChannelItem { Id = "test-id2" }
                        }
                    }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.GetUserChannels();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(
            new[]
            {
                new Channel("test-id1", ChannelType.User, messagingMock.Object, It.IsAny<string>(), new DisplayMetadata { Name = "1" }, It.IsAny<ILoggerFactory>()),
            });
    }

    [Fact]
    public async Task GetUserChannels_does_not_include_channel_if_displaymetadata_is_missing()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(
                JsonSerializer.Serialize(
                    new GetUserChannelsResponse
                    {
                        Channels = new[]
                        {
                            new ChannelItem { Id = "test-id1", DisplayMetadata = new DisplayMetadata { Name = "1" } },
                            new ChannelItem { Id = "test-id2" }
                        }
                    }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.GetUserChannels();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(
            new[]
            {
                new Channel("test-id1", ChannelType.User, messagingMock.Object, It.IsAny<string>(), new DisplayMetadata { Name = "1" }, It.IsAny<ILoggerFactory>()),
            });
    }

    [Fact]
    public async Task GetUserChannels_throws_error_on_null_channel_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(
                JsonSerializer.Serialize(
                    new GetUserChannelsResponse
                    {
                        Error = "test"
                    }, _jsonSerializerOptions)));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetUserChannels();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test*");
    }

    [Fact]
    public async Task GetOrCreateAppChannel_returns_channel()
    {
        var response = new CreateAppChannelResponse { Success = true };
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var channel = await desktopAgent.GetOrCreateChannel("testChannel");

        channel.Should().NotBeNull();
        channel.Id.Should().Be("testChannel");
    }

    [Fact]
    public async Task GetOrCreateAppChannel_throws_when_response_is_null()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.GetOrCreateChannel("testChannel");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task GetOrCreateAppChannel_throws_when_response_has_error()
    {
        var response = new CreateAppChannelResponse { Success = false, Error = "Some error" };
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.GetOrCreateChannel("testChannel");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task GetOrCreateAppChannel_throws_when_response_is_not_successful()
    {
        var response = new CreateAppChannelResponse { Success = false };
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.GetOrCreateChannel("testChannel");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*The application channel with ID: testChannel*");
    }

    [Fact]
    public async Task FindIntent_throws_when_response_is_null()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindIntent("fdc3.instrument");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server*");
    }

    [Fact]
    public async Task FindIntent_throws_when_error_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindIntentResponse { Error = "Some error" };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindIntent("fdc3.instrument");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindIntent_throws_when_no_error_but_AppIntent_is_null()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindIntentResponse { Error = null, AppIntent = null };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindIntent("fdc3.instrument");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*was not returned by the FDC3 DesktopAgent backend for intent: fdc3.instrument*");
    }

    [Fact]
    public async Task FindIntent_returns_AppIntent()
    {
        var messaginMock = new Mock<IMessaging>();
        var response = new FindIntentResponse { Error = null, AppIntent = new AppIntent { Intent = new IntentMetadata { Name = "test" }, Apps = new List<AppMetadata>() { new AppMetadata { AppId = "test-appId1" } } } };

        messaginMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messaginMock.Object);
        var result = await desktopAgent.FindIntent("fdc3.instrument");

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(response.AppIntent);
    }

    [Fact]
    public async Task FindInstances_returns_instances()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindInstancesResponse
        {
            Instances = new[]
            {
                new AppIdentifier
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    AppId = "test-appId1"
                },
                new AppIdentifier
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    AppId = "test-appId1"
                }
            }
        };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var result = await desktopAgent.FindInstances(new AppIdentifier { AppId = "test-appId1" });

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(response.Instances);
    }

    [Fact]
    public async Task FindInstances_throws_when_null_response()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?) null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var act = async () => await desktopAgent.FindInstances(new AppIdentifier { AppId = "test-appId1" });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server*");
    }

    [Fact]
    public async Task FindInstances_throws_when_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindInstancesResponse
        {
            Error = "Some error"
        };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindInstances(new AppIdentifier { AppId = "test-appId1" });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindInstances_throws_when_no_error_and_no_instances_received()
    {
        var response = new FindInstancesResponse
        {
            Error = null,
            Instances = null
        };

        var messagingMock = new Mock<IMessaging>();

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindInstances(new AppIdentifier { AppId = "test-appId1" });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No app matched*");
    }

    [Fact]
    public async Task FindIntentsByContext_returns_AppIntents()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindIntentsByContextResponse
        {
            AppIntents = new[]
            {
                new AppIntent
                {
                    Intent = new IntentMetadata { Name = "test-intent1" },
                    Apps = new List<AppMetadata> { new AppMetadata { AppId = "test-appId1" } }
                },
                new AppIntent
                {
                    Intent = new IntentMetadata { Name = "test-intent2" },
                    Apps = new List<AppMetadata> { new AppMetadata { AppId = "test-appId2" } }
                }
            }
        };

        var context = new Instrument(new InstrumentID { Ticker = "test-ticker" }, "test-name");

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.FindIntentsByContext(context);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(response.AppIntents);
    }

    [Fact]
    public async Task FindIntentsByContext_throws_when_null_response_received()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var act = async () => await desktopAgent.FindIntentsByContext(new Context("test-type"));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server*");
    }

    [Fact]
    public async Task FindIntentsByContext_throws_when_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindIntentsByContextResponse
        {
            Error = "Some error"
        };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);
        var act = async () => await desktopAgent.FindIntentsByContext(new Context("test-type"));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindIntentsByContext_throws_when_no_error_and_no_AppIntents_received()
    {
        var messagingMock = new Mock<IMessaging>();
        var response = new FindIntentsByContextResponse
        {
            Error = null,
            AppIntents = null
        };

        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var act = async () => await desktopAgent.FindIntentsByContext(new Context("test-type"));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*The AppIntent was not returned by*");
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