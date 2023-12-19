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
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;
using IntentMetadata = MorganStanley.Fdc3.AppDirectory.IntentMetadata;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class Fdc3DesktopAgentMessageRouterServiceTests
{
    private static readonly Mock<IMessageRouter> MockMessageRouter = new();
    private static readonly MockModuleLoader MockModuleLoader = new();
    private static readonly IAppDirectory AppDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions()
        {
            Source = new Uri($"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json")
        });

    private Fdc3DesktopAgentMessageRouterService _fdc3 = new(
        MockMessageRouter.Object,
        new Fdc3DesktopAgent(AppDirectory, MockModuleLoader.Object, new Fdc3DesktopAgentOptions(), NullLoggerFactory.Instance),
        new Fdc3DesktopAgentOptions(),
        NullLoggerFactory.Instance);

    private const string TestChannel = "testChannel";

    private JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new IntentMetadataJsonConverter(),
            new AppMetadataJsonConverter(),
        }
    };

    [Fact]
    public async void UserChannelAddedCanBeFound()
    {
        await _fdc3.HandleAddUserChannel(TestChannel);

        var result = await _fdc3.HandleFindChannel(Fdc3Topic.FindChannel, FindTestChannel, new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<FindChannelResponse>(_options).Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task RaiseIntent_fails_by_request_delivery_error()
    {
        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            null,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_Context()
    {
        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom")
        };

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_saves_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        MockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.AddIntentListener, It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        MockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        await MockModuleLoader.Object.StopModule(new(instance.InstanceId));
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            }, _options);

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = instanceId,
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2")
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new() { AppId = "appId4", Name = "app4", ResultType = null },
                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
            });
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context_if_fdc3_nothing()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = instanceId,
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("fdc3.nothing")
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new() { AppId = "appId4", Name = "app4", ResultType = null },
                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
            });
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "testIntent",
                Selected = false,
                Context = new Context("contextType"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "noAppShouldReturn" }
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Context()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("noAppShouldReturn")
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "noAppShouldReturn",
                Selected = false,
                Context = new Context("context2")
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_multiple_IAppIntents_found()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata8",
                Selected = false,
                Context = new Context("context7")
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_request_specifies_error()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "someIntent",
                Selected = false,
                Error = "Some weird error"
            }, _options);

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be("Some weird error");
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.HandleStoreIntentResult("dummy", null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request_contains_no_information()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "dummy",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = null,
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User,
        };

        var result = await _fdc3.HandleStoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_previosly_no_saved_raiseIntent_could_handle()
    {
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = "dummy",
            Intent = "dummy",
            OriginFdc3InstanceId = originFdc3InstanceId,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var action = async () => await _fdc3.HandleStoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>();
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse!.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await _fdc3.HandleStoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_context()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = true,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = new Context("test")
        };

        var result = await _fdc3.HandleStoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = null,
            VoidResult = true
        };

        var result = await _fdc3.HandleStoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_intent_not_found()
    {
        //Version should be the Intent's schema version
        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier() { AppId = "dummy", InstanceId = Guid.NewGuid().ToString() },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_InstanceId_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier() { AppId = "dummy" },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_no_intent_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var context = new Context("test");

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        }, _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId! },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_context()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        }, _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(context: context));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";

        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            ChannelType = channelType,
            ChannelId = channelId
        }, _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();

        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            VoidResult = true
        }, _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResponse.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResponse.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(voidResult: true));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_no_payload()
    {
        var result = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, null, new());
        result.Should().NotBeNull();
        result!.ReadJson<IntentListenerResponse>(_options).Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_missing_id()
    {
        var request = new IntentListenerRequest()
        {
            Intent = "dummy",
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            State = SubscribeState.Unsubscribe
        };
        var result = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, MessageBuffer.Factory.CreateJson(request, _options), new());
        result.Should().NotBeNull();
        result!.ReadJson<IntentListenerResponse>(_options).Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes_to_existing_raised_intent()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Selected = false,
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            }, _options);

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
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

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            }, _options);

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Selected = false,
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            }, _options);

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_unsubscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            }, _options);

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Unsubscribe
            }, _options);

        addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeFalse();
        addIntentListnerResponse!.Error.Should().BeNull();

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    private MessageBuffer FindTestChannel => MessageBuffer.Factory.CreateJson(new FindChannelRequest() { ChannelId = "testChannel", ChannelType = ChannelType.User });


    [Theory]
    [ClassData(typeof(FindIntentTheoryData))]
    public async Task FindIntent_edge_case_tests(FindIntentTestCase testCase)
    {
        var request = MessageBuffer.Factory.CreateJson(testCase.Request, _options);
        var result = await _fdc3.HandleFindIntent(Fdc3Topic.FindIntent, request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppCount > 0)
        {
            result!.ReadJson<FindIntentResponse>(_options)!.AppIntent!.Apps.Should().HaveCount(testCase.ExpectedAppCount);
        }

        result!.ReadJson<FindIntentResponse>(_options)!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    [Theory]
    [ClassData(typeof(FindIntentsByContextTheoryData))]
    public async Task FindIntentsByContext_edge_case_tests(FindIntentsByContextTestCase testCase)
    {
        var request = MessageBuffer.Factory.CreateJson(testCase.Request, _options);
        var result = await _fdc3.HandleFindIntentsByContext(Fdc3Topic.FindIntentsByContext, request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppIntentsCount > 0)
        {
            result!.ReadJson<FindIntentsByContextResponse>(_options)!.AppIntents!.Should().HaveCount(testCase.ExpectedAppIntentsCount);
        }

        result!.ReadJson<FindIntentsByContextResponse>(_options)!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    public class FindIntentsByContextTheoryData : TheoryData
    {
        public FindIntentsByContextTheoryData()
        {
            // Returning one AppIntent with one app by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("contextCustom")
                },//This relates to the appId4 only
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadataCustom", DisplayName = "intentMetadataCustom" },
                            Apps = new []
                            {
                                new AppMetadata(){ AppId ="appId4", Name = "app4", ResultType = null }
                            }
                        }
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple app by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context2")
                }, //This relates to the appId4, appId5, appId6,
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata4", DisplayName = "displayName4" },
                            Apps = new AppMetadata[]
                            {
                                new() { AppId = "appId4", Name = "app4", ResultType = null },
                                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
                            }
                        }
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple appIntent by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context9")
                },//This relates to the wrongappId9 and an another wrongAppId9 with 2 individual IntentMetadata
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata9", DisplayName = "displayName9" },
                            Apps = new []
                            {
                                new AppMetadata()
                                {
                                    AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp"
                                },
                            }
                        },
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata10", DisplayName = "displayName10" },
                            Apps = new []
                            {
                                new AppMetadata(){ AppId = "appId11", Name = "app11", ResultType = "channel<specified>" },
                            }
                        },
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata11", DisplayName = "displayName11" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"},
                            }
                        }
                    }
                },
                ExpectedAppIntentsCount = 3
            });

            // Returning error no apps found by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("noAppShouldReturn")
                },// no app should have this context type
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.NoAppsFound
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning one AppIntent with one app by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context2"), //This relates to multiple appId
                    ResultType = "resultType<specified>"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(){
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata4", DisplayName = "displayName4" }, // it should just return appId5
                            Apps = new []
                            {
                                new AppMetadata(){ AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" }
                            }
                        }
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple apps by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context2"), //This relates to multiple appId
                    ResultType = "resultType"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata4", DisplayName = "displayName4" },
                            Apps = new[]
                            {
                                new AppMetadata(){ AppId ="appId5", Name = "app5", ResultType = "resultType<specified>" },
                                new AppMetadata(){ AppId ="appId6", Name = "app6", ResultType = "resultType" }
                            }
                        },
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple AppIntent by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context9"), //This relates to multiple appId
                    ResultType = "resultWrongApp"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata9", DisplayName = "displayName9" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp" },
                            }
                        },
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata11", DisplayName = "displayName11" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp" }
                            }
                        }
                    }
                },
                ExpectedAppIntentsCount = 2
            });

            // Returning no apps found error by using ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("context9"), //This relates to multiple appId
                    ResultType = "noAppShouldReturn"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.NoAppsFound
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning intent delivery error
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = null,
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.IntentDeliveryFailed
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning all the apps that are using the ResultType by adding fdc3.nothing.
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context("fdc3.nothing"),
                    ResultType = "resultWrongApp"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata9", DisplayName = "displayName9" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp" },
                            }
                        },

                        new AppIntent()
                        {
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata11", DisplayName = "displayName11" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp" }
                            }
                        },
                    }
                },
                ExpectedAppIntentsCount = 2
            });
        }
    }

    public class FindIntentsByContextTestCase
    {
        internal FindIntentsByContextRequest Request { get; set; }
        internal FindIntentsByContextResponse ExpectedResponse { get; set; }
        public int ExpectedAppIntentsCount { get; set; }
    }

    private class FindIntentTheoryData : TheoryData
    {
        public FindIntentTheoryData()
        {
            //As per the documentation : https://github.com/morganstanley/fdc3-dotnet/blob/main/src/Fdc3/IIntentMetadata.cs
            //name is unique for the intents, so it should be unique for every app, or the app should have the same intentMetadata?
            //if so we should return multiple appIntents and do not return error message for the client.
            //We have setup a test case for wrongappId9 which contains wrongly setted up intentMetadata.
            AddRow(
                new FindIntentTestCase()
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse()
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    },
                    Request = new FindIntentRequest()
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata8"
                    }
                });

            AddRow(
                new FindIntentTestCase()
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse()
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    Request = new FindIntentRequest()
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata2",
                        Context = new Context("noAppShouldBeReturned")
                    }
                });

            AddRow(
                new FindIntentTestCase()
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse()
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    },
                    Request = null
                });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata7",
                    Context = new Context("context8"),
                    ResultType = "resultType2<specified2>"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadat7", DisplayName = "displayName7" },
                        Apps = new[]
                        {
                            new AppMetadata(){ AppId = "appId7", Name = "app7", ResultType = "resultType2<specified2>" }
                        }
                    }
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata4",
                    Context = new Context("context2"),
                    ResultType = "resultType"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata4", DisplayName = "displayName4" },
                        Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                            new AppMetadata() { AppId = "appId6", Name = "app6", ResultType = "resultType"},

                        }
                    }
                },
                ExpectedAppCount = 2
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata7",
                    ResultType = "resultType2<specified2>"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadat7", DisplayName = "displayName7" },
                        Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId7", Name = "app7", ResultType = "resultType2<specified2>"}
                        }
                    }
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata4",
                    ResultType = "resultType"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata4", DisplayName = "displayName4" },
                        Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                            new AppMetadata() { AppId = "appId6", Name = "app6", ResultType = "resultType" },
                        }
                    }
                },
                ExpectedAppCount = 2
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata1",
                    Context = new Context("context1")
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata1", DisplayName = "displayName1" },
                        Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId1", Name = "app1", ResultType = null }
                        }
                    }
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata4",
                    Context = new Context("context2")
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata4", DisplayName = "displayName4" },
                        Apps = new AppMetadata[]
                            {
                                new() { AppId = "appId4", Name = "app4", ResultType = null },
                                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
                            }
                    }
                },
                ExpectedAppCount = 3
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata2"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata2", DisplayName = "displayName2" },
                        Apps = new[]
                        {
                            new AppMetadata() { AppId = "appId2", Name = "app2", ResultType = null }
                        }
                    }
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Intent = "intentMetadata4"
                },
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent()
                    {
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata4", DisplayName = "displayName4" },
                        Apps = new AppMetadata[]
                            {
                                new() { AppId = "appId4", Name = "app4", ResultType = null },
                                new() { AppId = "appId5", Name = "app5", ResultType = "resultType<specified>" },
                                new() { AppId = "appId6", Name = "app6", ResultType = "resultType" }
                            }
                    }
                },
                ExpectedAppCount = 3
            });
        }
    }

    public class FindIntentTestCase
    {
        internal FindIntentRequest Request { get; set; }
        internal FindIntentResponse ExpectedResponse { get; set; }
        public int ExpectedAppCount { get; set; }
    }
}
