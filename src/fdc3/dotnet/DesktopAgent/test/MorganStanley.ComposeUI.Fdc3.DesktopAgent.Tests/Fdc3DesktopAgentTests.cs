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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;
using AppChannel = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels.AppChannel;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.Icon;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ImplementationMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public partial class Fdc3DesktopAgentTests : Fdc3DesktopAgentTestsBase
{
    public Fdc3DesktopAgentTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task AddUserChannel_wont_throw_and_adds_channel()
    {
        var mockMessageService = new Mock<IMessaging>();

        var action = async () => await Fdc3.AddUserChannel((channelId) => new Mock<UserChannel>(
            channelId,
            mockMessageService.Object,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            NullLogger<UserChannel>.Instance).Object, "fdc3.channel.1");

        await action.Should().NotThrowAsync();

        var channelExists = Fdc3.FindChannel(channelId: "fdc3.channel.1", ChannelType.User);
        channelExists.Should().BeTrue();
    }

    [Fact]
    public async Task AddPrivateChannel_returns_null_with_no_channelId_passed()
    {
        var mockMessageService = new Mock<IMessaging>();

        var action = async () => await Fdc3.CreateOrJoinPrivateChannel(
                (channelId) =>
                new Mock<PrivateChannel>(
                    channelId,
                    mockMessageService.Object,
                    NullLogger<UserChannel>.Instance).Object,
                null!, null!);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAppChannel_returns_null_with_no_channelId_passed()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var mockMessageService = new Mock<IMessaging>();

        var action = async () => await Fdc3.AddAppChannel(
                (channelId) =>
                new Mock<AppChannel>(
                    channelId,
                    mockMessageService.Object,
                    NullLogger<UserChannel>.Instance).Object,
                new CreateAppChannelRequest
                {
                    ChannelId = null!,
                    InstanceId = originFdc3InstanceId
                });

        await action.Should().NotThrowAsync();

        var result = await action();
        result.Should().NotBeNull();
        result!.Error.Should().Be(ChannelError.CreationFailed);
    }

    [Fact]
    public void FindChannel_returns_false()
    {
        var result = Fdc3.FindChannel(channelId: "testChannelId", ChannelType.User);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FindChannel_returns_true()
    {
        var mockMessageService = new Mock<IMessaging>();

        await Fdc3.AddUserChannel((channelId) => new Mock<UserChannel>(
            channelId,
            mockMessageService.Object,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            NullLogger<UserChannel>.Instance).Object, "fdc3.channel.1");

        var result = Fdc3.FindChannel(channelId: "fdc3.channel.1", ChannelType.User);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetIntentResult_returns()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var resultContext = new Context(ResultType1);
        var target = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeTrue();

        var raiseIntentRequest =
            new RaiseIntentRequest
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = Intent1.Name,
                Context = SingleContext.AsJson(),
                TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResponse = await Fdc3.RaiseIntent(raiseIntentRequest, SingleContext.Type);

        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse.Response.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResponse.Response.MessageId!,
            Intent = Intent1.Name,
            OriginFdc3InstanceId = raiseIntentResponse.Response.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = resultContext.AsJson()
        };

        var storeResult = await Fdc3.StoreIntentResult(storeIntentRequest);
        storeResult.Should().NotBeNull();

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResponse.Response.MessageId!,
            Intent = Intent1.Name,
            TargetAppIdentifier = new AppIdentifier
            { AppId = App1.AppId, InstanceId = raiseIntentResponse.Response.AppMetadata!.InstanceId! }
        };

        var result = await Fdc3.GetIntentResult(getIntentResultRequest);
        result.Should().NotBeNull();
        result.Context.Should().Be(resultContext.AsJson());
    }

    [Fact]
    public async Task GetIntentResult_fails()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var target = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var resultContext = new Context("resultType1");

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResponse = await Fdc3.RaiseIntent(raiseIntentRequest, SingleContext.Type);
        raiseIntentResponse.Response.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResponse.Response.MessageId!,
            Intent = Intent1.Name,
            OriginFdc3InstanceId = raiseIntentResponse.Response.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = resultContext.AsJson()
        };

        var storeResponse = await Fdc3.StoreIntentResult(storeIntentRequest);
        storeResponse.Error.Should().BeNull();

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResponse.Response.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier
            { AppId = App1.AppId, InstanceId = raiseIntentResponse.Response.AppMetadata!.InstanceId! },
            Version = "1.0"
        };

        var result = await Fdc3.GetIntentResult(getIntentResultRequest);
        result.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task StoreIntentResult_fails_as_id_is_missing()
    {
        var request = new StoreIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            OriginFdc3InstanceId = Guid.NewGuid().ToString(),
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await Fdc3.StoreIntentResult(request);

        result.Should().BeEquivalentTo(StoreIntentResultResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task StoreIntentResult_returns()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var target = await ModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResponse = await Fdc3.RaiseIntent(raiseIntentRequest, MultipleContext.Type);
        raiseIntentResponse.Response.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResponse!.Response.MessageId!,
            Intent = Intent2.Name,
            OriginFdc3InstanceId = raiseIntentResponse.Response.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await Fdc3.StoreIntentResult(storeIntentRequest);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new StoreIntentResultResponse { Stored = true });
    }

    [Fact]
    public async Task AddIntentListener_subscribes()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await ModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App4.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResponse = await Fdc3.RaiseIntent(raiseIntentRequest, ContextTypes.Nothing);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse.Response.AppMetadata.Should().NotBeNull();
        raiseIntentResponse.Response.AppMetadata!.AppId.Should().Be(App4.AppId);
        raiseIntentResponse.Response.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);
        raiseIntentResponse.RaiseIntentResolutionMessages.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddIntentListener_unsubscribes()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await ModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var addIntentListenerRequest1 = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse1 = await Fdc3.AddIntentListener(addIntentListenerRequest1);
        addIntentListenerResponse1.Should().NotBeNull();
        addIntentListenerResponse1.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App4.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResponse = await Fdc3.RaiseIntent(raiseIntentRequest, ContextTypes.Nothing);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse.Response.AppMetadata.Should().NotBeNull();
        raiseIntentResponse.Response.AppMetadata!.AppId.Should().Be(App4.AppId);
        raiseIntentResponse.Response.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);
        raiseIntentResponse.RaiseIntentResolutionMessages.Should().NotBeEmpty();

        var addIntentListenerRequest2 = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Unsubscribe
        };

        var addIntentListenerResponse2 = await Fdc3.AddIntentListener(addIntentListenerRequest2);
        addIntentListenerResponse2.Should().NotBeNull();
        addIntentListenerResponse2.Stored.Should().BeFalse();
    }

    [Fact]
    public async Task AddIntentListener_unsubscribe_fails()
    {
        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            State = SubscribeState.Unsubscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeFalse();
        addIntentListenerResponse.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task AppChannel_is_created()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var mockMessaging = new Mock<IMessaging>();

        var result = await Fdc3.AddAppChannel((channelId) => new AppChannel(
            channelId,
            mockMessaging.Object,
            new JsonSerializerOptions(),
            new Mock<ILogger<AppChannel>>().Object), new CreateAppChannelRequest() { ChannelId = "my.channelId", InstanceId = originFdc3InstanceId });

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Created());
    }

    [Fact]
    public async Task GetUserChannels_returns_payload_null_error()
    {
        GetUserChannelsRequest? request = null;
        var result = await Fdc3.GetUserChannels(request);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task GetUserChannels_returns_missing_id_error()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = "NotValidId"
        };

        var result = await Fdc3.GetUserChannels(request);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task GetUserChannels_returns_access_denied_error()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = Guid.NewGuid().ToString()
        };

        var result = await Fdc3.GetUserChannels(request);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(ChannelError.AccessDenied));
    }

    [Fact]
    public async Task GetUserChannels_returns_empty_userChannel_set()
    {
        var options = new Fdc3DesktopAgentOptions
        {
            UserChannelConfigFile = new Uri("C://hello/world/test.json"),
        };

        var fdc3 = new Fdc3DesktopAgent(
            AppDirectory,
            ModuleLoader.Object,
            options,
            ResolverUICommunicator.Object,
            new UserChannelSetReader(options));

        await fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetUserChannelsRequest
        {
            InstanceId = originFdc3InstanceId
        };

        var result = await fdc3.GetUserChannels(request);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Success(Enumerable.Empty<ChannelItem>()));

        await fdc3.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetUserChannels_succeeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetUserChannelsRequest
        {
            InstanceId = originFdc3InstanceId
        };

        var result = await Fdc3.GetUserChannels(request);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Success(new List<ChannelItem>() {
            new() { Id = "fdc3.channel.1", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 1", Color = "red", Glyph = "1" } },
            new() { Id = "fdc3.channel.2", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 2", Color = "orange", Glyph = "2" } },
            new() { Id = "fdc3.channel.3", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 3", Color = "yellow", Glyph = "3" } },
            new() { Id = "fdc3.channel.4", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 4", Color = "green", Glyph = "4" }},
            new() { Id = "fdc3.channel.5", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 5", Color = "cyan", Glyph = "5" } },
            new() { Id = "fdc3.channel.6", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 6", Color = "blue", Glyph = "6" } },
            new() { Id = "fdc3.channel.7", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 7", Color = "magenta", Glyph = "7" } },
            new() { Id = "fdc3.channel.8", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 8", Color = "purple", Glyph = "8" } }
        }));
    }

    [Fact]
    public async Task JoinUserChannel_returns_missing_id_error_as_instance_id_not_found()
    {
        var result = await Fdc3.JoinUserChannel((channelId) => new UserChannel(channelId, new Mock<IMessaging>().Object, new JsonSerializerOptions(), null), new() { InstanceId = Guid.NewGuid().ToString(), ChannelId = "test" });

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task JoinUserChannel_returns_no_channel_found_error_as_channel_id_not_found()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var channel = new UserChannel("test", new Mock<IMessaging>().Object, new JsonSerializerOptions(), null);
        var result = await Fdc3.JoinUserChannel((channelId) => new UserChannel(channelId, new Mock<IMessaging>().Object, new JsonSerializerOptions(), null), new() { InstanceId = originFdc3InstanceId, ChannelId = "test" });

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(ChannelError.NoChannelFound));
    }

    [Fact]
    public async Task JoinUserChannel_succeeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var result = await Fdc3.JoinUserChannel((channelId) => new UserChannel(channelId, new Mock<IMessaging>().Object, new JsonSerializerOptions(), null), new() { InstanceId = originFdc3InstanceId, ChannelId = "fdc3.channel.1" });

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Joined(new DisplayMetadata()
        {
            Color = "red",
            Glyph = "1",
            Name = "Channel 1"
        }));
    }

    [Fact]
    public async Task GetInfo_fails_as_no_payload_received()
    {
        GetInfoRequest? request = null;

        var result = await Fdc3.GetInfo(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetInfo_fails_as_no_instanceId_received()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = null
            }
        };

        var result = await Fdc3.GetInfo(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfo_fails_as_not_valid_instanceId_received()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = "NotExistentNotParsableGuidId"
            }
        };

        var result = await Fdc3.GetInfo(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfo_fails_as_instanceId_missing_from_running_modules()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = Guid.NewGuid().ToString(),
            }
        };

        var result = await Fdc3.GetInfo(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfo_succeeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = originFdc3InstanceId,
            }
        };

        var result = await Fdc3.GetInfo(request);

        result.Should().NotBeNull();
        result.ImplementationMetadata.Should().NotBeNull();
        result.ImplementationMetadata
            .Should()
            .BeEquivalentTo(new ImplementationMetadata()
            {
                AppMetadata = new AppMetadata
                {
                    AppId = App1.AppId,
                    InstanceId = originFdc3InstanceId,
                    Description = null,
                    Icons = Enumerable.Empty<Icon>(),
                    Name = App1.Name,
                    ResultType = null,
                    Screenshots = Enumerable.Empty<Screenshot>(),
                    Title = null,
                    Tooltip = null,
                    Version = null
                },
                Fdc3Version = Constants.SupportedFdc3Version,
                OptionalFeatures = new OptionalDesktopAgentFeatures
                {
                    OriginatingAppMetadata = false,
                    UserChannelMembershipAPIs = Constants.SupportUserChannelMembershipAPI
                },
                Provider = Constants.DesktopAgentProvider,
                ProviderVersion = Constants.ComposeUIVersion ?? "0.0.0"
            });
    }

    [Fact]
    public async Task FindInstances_returns_PayloadNull_error_as_no_request()
    {
        FindInstancesRequest? request = null;

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task FindInstances_returns_MissingId_as_invalid_id()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = "notValidInstanceId",
        };

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task FindInstances_returns_MissingId_error_as_no_instance_found_which_is_contained_by_the_container()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task FindInstances_returns_NoAppsFound_error_as_no_appId_found()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "noAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindInstances_succeeds_with_one_app()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Instances.Should().HaveCount(1);
        result.Instances!.ElementAt(0).InstanceId.Should().Be(originFdc3InstanceId);
    }

    [Fact]
    public async Task FindInstances_succeeds_with_empty_array()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId2",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await Fdc3.FindInstances(request);

        result.Should().NotBeNull();
        result.Instances.Should().HaveCount(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetAppMetadata_returns_PayLoadNull_error_as_request_null()
    {
        GetAppMetadataRequest? request = null;

        var result = await Fdc3.GetAppMetadata(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetAppMetadata_returns_MissingId_error_as_initiator_id_not_found()
    {
        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString(),
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetAppMetadata_returns_MissingId_error_as_the_searched_instanceId_not_valid()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = "notValidInstanceId"
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetAppMetadata_returns_TargetInstanceUnavailable_error_as_the_searched_instanceId_not_found()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = Guid.NewGuid().ToString()
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.TargetInstanceUnavailable);
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppMetadata_based_on_instanceId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = originFdc3InstanceId
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Error.Should().BeNull();
        result.AppMetadata.Should().BeEquivalentTo(
            new AppMetadata()
            {
                AppId = "appId1",
                InstanceId = originFdc3InstanceId,
                Name = "app1"
            });
    }

    [Fact]
    public async Task GetAppMetadata_returns_TargetAppUnavailable_error_as_the_searched_appId_not_found()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "notExistentAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Error.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.TargetAppUnavailable);
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppMetadata_based_on_appId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await Fdc3.GetAppMetadata(request);

        result.Error.Should().BeNull();
        result.AppMetadata.Should().BeEquivalentTo(
            new AppMetadata()
            {
                AppId = "appId1",
                Name = "app1"
            });
    }

    [Fact]
    public async Task AddContextListener_returns_payload_null_error()
    {
        AddContextListenerRequest? request = null;

        var response = await Fdc3.AddContextListener(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task AddContextListener_returns_missing_id_error()
    {
        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = "dummyId",
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var response = await Fdc3.AddContextListener(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task AddContextListener_successfully_registers_context_listener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var response = await Fdc3.AddContextListener(request);

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveContextListener_returns_payload_null_error()
    {
        RemoveContextListenerRequest? request = null;

        var response = await Fdc3.RemoveContextListener(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task RemoveContextListener_returns_missing_id_error()
    {
        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = "dummyId",
            ListenerId = Guid.NewGuid().ToString(),
        };

        var response = await Fdc3.RemoveContextListener(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task RemoveContextListener_returns_listener_not_found_error()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = "fdc3.instrument"
        };

        var addContextListenerResponse = await Fdc3.AddContextListener(addContextListenerRequest);
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var response = await Fdc3.RemoveContextListener(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.ListenerNotFound);
    }

    [Fact]
    public async Task RemoveContextListener_successfully_removes_context_listener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = null
        };

        var addContextListenerResponse = await Fdc3.AddContextListener(addContextListenerRequest);
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var response = await Fdc3.RemoveContextListener(request);

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Open_returns_PayloadNull_error()
    {
        OpenRequest? request = null;

        var response = await Fdc3.Open(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task Open_returns_MissingId_error()
    {
        OpenRequest? request = new()
        {
            InstanceId = "NotExistentId"
        };

        var response = await Fdc3.Open(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task Open_returns_AppNotFound_error()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);
        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier()
            {
                AppId = "NonExistentAppId"
            }
        };

        var response = await Fdc3.Open(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenError.AppNotFound);
    }

    [Fact]
    public async Task Open_returns_AppTimeout_error_as_context_listener_is_not_registered()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);
        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier()
            {
                AppId = App1.AppId
            },
            Context = JsonSerializer.Serialize(new Context("fdc3.instrument"))
        };

        var response = await Fdc3.Open(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenError.AppTimeout);
    }

    [Fact]
    public async Task Open_returns_without_context()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        //ChannelId property is just injected on the window object if the app should join to a channel -sending the channel id that the app was on which sent the request.
        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId
            }
        };

        var response = await Fdc3.Open(request);

        response.Should().NotBeNull();
        response!.Error.Should().BeNull();
        response!.AppIdentifier.Should().NotBeNull();
        response!.AppIdentifier!.AppId.Should().Be(App1.AppId);
        response!.AppIdentifier!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOpenedAppContext_returns_PayloadNull_error()
    {
        GetOpenedAppContextRequest? request = null;

        var response = await Fdc3.GetOpenedAppContext(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetOpenedAppContext_returns_IdNotParsable_error()
    {
        GetOpenedAppContextRequest? request = new()
        {
            ContextId = "NotValidId"
        };

        var response = await Fdc3.GetOpenedAppContext(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.IdNotParsable);
    }

    [Fact]
    public async Task GetOpenedAppContext_returns_ContextNotFound_error()
    {
        GetOpenedAppContextRequest? request = new()
        {
            ContextId = Guid.NewGuid().ToString(),
        };

        var response = await Fdc3.GetOpenedAppContext(request);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.OpenedAppContextNotFound);
    }
}