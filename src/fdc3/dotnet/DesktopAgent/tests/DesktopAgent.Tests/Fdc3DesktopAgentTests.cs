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


using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.Fdc3.AppDirectory;
using IntentMetadata = MorganStanley.Fdc3.AppDirectory.IntentMetadata;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure;
using MorganStanley.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3DesktopAgentTests
{
    private static readonly MockModuleLoader MockModuleLoader = new();
    private static readonly IAppDirectory AppDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions()
        {
            Source = new Uri($"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json")
        });
    private readonly IFdc3DesktopAgentBridge _fdc3 = new Fdc3DesktopAgent(AppDirectory, MockModuleLoader.Object, new Fdc3DesktopAgentOptions(), NullLoggerFactory.Instance);

    [Fact]
    public async Task AddUserChannel_wont_throw()
    {
        var mockMessageService = new Mock<IMessagingService>();
        mockMessageService.Setup(_ => _.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) => ValueTask.CompletedTask);

        var mockUserChannel = new Mock<UserChannel>(
            It.IsAny<string>(),
            mockMessageService.Object,
            NullLogger<UserChannel>.Instance);

        var action = async () => await _fdc3.AddUserChannel(mockUserChannel.Object);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void FindChannel_returns_false()
    {
        var result = _fdc3.FindChannel("testChannelId", ChannelType.User);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FindChannel_returns_true()
    {
        var mockMessageService = new Mock<IMessagingService>();
        mockMessageService.Setup(_ => _.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) => ValueTask.CompletedTask);

        var mockUserChannel = new Mock<UserChannel>(
            "testChannelId",
            mockMessageService.Object,
            NullLogger<UserChannel>.Instance);

        await _fdc3.AddUserChannel(mockUserChannel.Object);
        var result = _fdc3.FindChannel("testChannelId", ChannelType.User);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound()
    {
        var request = new FindIntentRequest()
        {
            Intent = "testIntent",
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _fdc3.FindIntent(request);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntent_returns_IntentDeliveryFailed()
    {
        var request = new FindIntentRequest()
        {
            Intent = "intentMetadata8", //wrongly setup MockAppDirectory in purpose
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _fdc3.FindIntent(request);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task FindIntent_returns()
    {
        var request = new FindIntentRequest()
        {
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            ResultType = "resultType",
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _fdc3.FindIntent(request);
        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(
            new AppIntent()
            {
                Intent = new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2", "context5" }),
                Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                            new AppMetadata() { AppId = "appId6", Name = "app6", ResultType = "resultType"},

                        }
            });
    }

    [Fact]
    public async Task FindIntentsByContext_returns_NoAppsFound()
    {
        var request = new FindIntentsByContextRequest()
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = new Context("context9"), //This relates to multiple appId
            ResultType = "noAppShouldReturn"
        };
        var result = await _fdc3.FindIntentsByContext(request);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntentsByContext_returns()
    {
        var request = new FindIntentsByContextRequest()
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = new Context("fdc3.nothing"),
            ResultType = "resultWrongApp"
        };
        var result = await _fdc3.FindIntentsByContext(request);
        result.Should().NotBeNull();
        result.AppIntents.Should().BeEquivalentTo(
            new[]
                    {
                        new AppIntent()
                        {
                            Intent = new IntentMetadata("intentMetadata9", "displayName9", new [] { "context9" }),
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp" },
                            }
                        },

                        new AppIntent()
                        {
                            Intent = new IntentMetadata("intentMetadata11", "displayName11", new [] { "context9" }),
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp" }
                            }
                        },
                    });
    }

    [Fact]
    public async Task GetIntentResult_returns()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest =
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResponse = await _fdc3.RaiseIntent(raiseIntentRequest);

        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.Key.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.Key.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.Key.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResult = await _fdc3.StoreIntentResult(storeIntentRequest);
        storeResult.Should().NotBeNull();

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.Key.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.Key.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.GetIntentResult(getIntentResultRequest);
        result.Should().NotBeNull();
        result.Context.Should().Be(context);

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_fails()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var context = new Context("test");

        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResponse = await _fdc3.RaiseIntent(raiseIntentRequest);
        raiseIntentResponse!.Key.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.Key.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.Key.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResponse = await _fdc3.StoreIntentResult(storeIntentRequest);
        storeResponse.Error.Should().BeNull();

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.Key.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.Key.AppMetadata!.First().InstanceId! },
            Version = "1.0"
        };

        var result = await _fdc3.GetIntentResult(getIntentResultRequest);
        result.Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task StoreIntentResult_throws()
    {
        var request = new StoreIntentResultRequest()
        {
            MessageId = "dummy",
            Intent = "dummy",
            OriginFdc3InstanceId = Guid.NewGuid().ToString(),
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var action = async () => await _fdc3.StoreIntentResult(request);

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>();
    }

    [Fact]
    public async Task StoreIntentResult_returns()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResponse = await _fdc3.RaiseIntent(raiseIntentRequest);
        raiseIntentResponse!.Key.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse!.Key.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.Key.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await _fdc3.StoreIntentResult(storeIntentRequest);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Selected = false,
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResponse = await _fdc3.RaiseIntent(raiseIntentRequest);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.Key.AppMetadata.Should().HaveCount(1);
        raiseIntentResponse!.Key.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResponse!.Key.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);
        raiseIntentResponse!.Value.Should().BeNull();

        var addIntentListenerRequest = new AddIntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListenerResponse = await _fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();

        addIntentListenerResponse!.Key.Stored.Should().BeTrue();
        addIntentListenerResponse!.Value.Should().NotBeNull();

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_unsubscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResponse = await _fdc3.RaiseIntent(raiseIntentRequest);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.Key.AppMetadata.Should().HaveCount(1);
        raiseIntentResponse!.Key.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResponse!.Key.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);
        raiseIntentResponse!.Value.Should().BeNull();

        var addIntentListenerRequest1 = new AddIntentListenerRequest()
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse1 = await _fdc3.AddIntentListener(addIntentListenerRequest1);
        addIntentListenerResponse1.Should().NotBeNull();
        addIntentListenerResponse1!.Key.Stored.Should().BeTrue();
        addIntentListenerResponse1!.Value.Should().NotBeNull();

        var addIntentListenerRequest2 = new AddIntentListenerRequest()
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Unsubscribe
        };

        var addIntentListenerResponse2 = await _fdc3.AddIntentListener(addIntentListenerRequest2);
        addIntentListenerResponse2.Should().NotBeNull();
        addIntentListenerResponse2!.Key.Stored.Should().BeFalse();
        addIntentListenerResponse2!.Value.Should().BeNull();

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_unsubscribe_fails()
    {
        var addIntentListenerRequest = new AddIntentListenerRequest()
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            State = SubscribeState.Unsubscribe
        };

        var addIntentListenerResponse = await _fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse!.Key.Stored.Should().BeFalse();
        addIntentListenerResponse!.Value.Should().BeNull();
        addIntentListenerResponse!.Key.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task RaiseIntent_returns_NoAppsFound()
    {
        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "noAppShouldReturn",
            Selected = false,
            Context = new Context("context2")
        };

        var result = await _fdc3.RaiseIntent(request);
        result.Should().NotBeNull();
        result!.Key.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_returns_IntentDeliveryFailed()
    {
        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata8",
            Selected = false,
            Context = new Context("context7")
        };

        var result = await _fdc3.RaiseIntent(request);
        result.Should().NotBeNull();
        result!.Key.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_request_specifies_error()
    {
        var request = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "someIntent",
                Selected = false,
                Error = "Some weird error"
            };

        var result = await _fdc3.RaiseIntent(request);

        result!.Key.Error.Should().Be("Some weird error");
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps()
    {
        var request =new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("fdc3.nothing")
            };

        var result = await _fdc3.RaiseIntent(request);
        result.Should().NotBeNull();
        result!.Value.Should().BeNull();
        result!.Key!.AppMetadata.Should().HaveCount(3);
        result!.Key.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new() { AppId = "appId4", Name = "app4", ResultType = null },
                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
            });
    }

    [Fact]
    public async Task RaiseIntent_fails_as_multiple_AppIntent_found()
    {
        var request =new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata8", //wrongly setup AppDirectory on purpose
                Selected = false,
                Context = new Context("context7")
            };

        var result = await _fdc3.RaiseIntent(request);
        result.Should().NotBeNull();
        result!.Key.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_running_app()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new AddIntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListnerResponse = await _fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListnerResponse.Should().NotBeNull();
        addIntentListnerResponse!.Key.Stored.Should().BeTrue();
        addIntentListnerResponse.Value.Should().BeNull();

        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.RaiseIntent(request);

        result.Should().NotBeNull();
        result!.Key.AppMetadata.Should().HaveCount(1);
        result!.Key.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.Key.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);
        result.Value.Should().NotBeNull();
        result!.Value!.Intent.Should().Be("intentMetadataCustom");
        result!.Value!.TargetModuleInstanceId.Should().Be(targetFdc3InstanceId);

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }
}
