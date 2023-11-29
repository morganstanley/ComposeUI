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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;
using IntentMetadata = MorganStanley.Fdc3.AppDirectory.IntentMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3DesktopAgentTests
{
    private static readonly Mock<IMessageRouter> MockMessageRouter = new();
    private static readonly MockModuleLoader MockModuleLoader = new();
    private static readonly IAppDirectory AppDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions()
        {
            Source = new Uri($"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json")
        });

    private Fdc3DesktopAgent _fdc3 = new(
        AppDirectory,
        MockModuleLoader.Object,
        new Fdc3DesktopAgentOptions(),
        MockMessageRouter.Object,
        NullLoggerFactory.Instance);

    private const string TestChannel = "testChannel";

    private JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new IIntentMetadataJsonConverter(),
            new IAppMetadataJsonConverter(),
            new AppMetadataJsonConverter(), //TODO: remove converter when no longer is necessary
        }
    };

    [Fact]
    public async void UserChannelAddedCanBeFound()
    {
        await _fdc3.AddUserChannel(TestChannel);

        var result = await _fdc3.FindChannel(Fdc3Topic.FindChannel, FindTestChannel, new MessageContext());

        result.Should().NotBeNull();
        result.ReadJson<FindChannelResponse>().Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4")));

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
        var request = new RaiseIntentRequest(
            messageId: 1,
            fdc3InstanceId: Guid.NewGuid().ToString(),
            intent: "intentMetadataCustom",
            selected: false,
            context: new Context("contextCustom"));

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

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
        await _fdc3.SubscribeAsync();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new RaiseIntentRequest(
            messageId: 1,
            fdc3InstanceId: Guid.NewGuid().ToString(),
            intent: "intentMetadataCustom",
            selected: false,
            context: new Context("contextCustom"),
            targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId));

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
            _ => _.InvokeAsync(Fdc3Topic.AddIntentListener, It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        MockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        await MockModuleLoader.Object.StopModule(new(instance.InstanceId));
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.SubscribeAsync();

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: "intentMetadataCustom",
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest(
            messageId: 1,
            fdc3InstanceId: originFdc3InstanceId,
            intent: "intentMetadataCustom",
            selected: false,
            context: new Context("contextCustom"),
            targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId));

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
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: instanceId,
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                null,
                null));

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new("appId4", name: "app4", resultType: null),
                new("appId5", name: "app5", resultType: "resultType<specified>"),
                new("appId6", name: "app6", resultType: "resultType")
            });
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context_if_fdc3_nothing()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: instanceId,
                intent: "intentMetadata4",
                selected: false,
                context: new Context("fdc3.nothing"),
                null,
                null));

        var result = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new("appId4", name: "app4", resultType: null),
                new("appId5", name: "app5", resultType: "resultType<specified>"),
                new("appId6", name: "app6", resultType: "resultType")
            });
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "testIntent",
                selected: false,
                context: new Context("contextType"),
                targetAppIdentifier: new AppIdentifier("noAppShouldReturn"),
                null));

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
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("noAppShouldReturn"),
                null,
                null));

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
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "noAppShouldReturn",
                selected: false,
                context: new Context("context2"),
                null,
                null));

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
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata8",
                selected: false,
                context: new Context("context7"),
                null,
                null));

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
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "someIntent",
                selected: false,
                context: null,
                null,
                error: "Some weird error"));

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
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4")));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "dummy",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: null,
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var result = await _fdc3.HandleStoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_previosly_no_saved_raiseIntent_could_handle()
    {
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var storeIntentRequest = new StoreIntentResultRequest(
            messageId: "dummy",
            intent: "dummy",
            originFdc3InstanceId: originFdc3InstanceId,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var action = async () => await _fdc3.HandleStoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>();
    }


    [Fact]
    public async Task StoreIntentResult_succeeds_with_channel()
    {
        await _fdc3.SubscribeAsync();
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            messageId: raiseIntentResponse!.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var result = await _fdc3.HandleStoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_context()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: true,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4")));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: null,
            channelType: null,
            context: new Context("test"));

        var result = await _fdc3.HandleStoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.SubscribeAsync();
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: null,
            channelType: null,
            context: null,
            voidResult: "dummy error happened during Promise");

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
        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: "dummy",
             intent: "dummy",
             targetAppIdentifier: new AppIdentifier("dummy", Guid.NewGuid().ToString()),
             version: "1.0");

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_InstanceId_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: "dummy",
             intent: "dummy",
             targetAppIdentifier: new AppIdentifier("dummy"),
             version: "1.0");

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_no_intent_found()
    {
        await _fdc3.SubscribeAsync();
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var context = new Context("test");

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: originFdc3InstanceId,
            context: context), _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: raiseIntentResponse.MessageId!,
             intent: "dummy",
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!),
             version: "1.0");

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_context()
    {
        await _fdc3.SubscribeAsync();
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: originFdc3InstanceId,
            context: context), _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: raiseIntentResponse.MessageId!,
             intent: "intentMetadata4",
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(context: context));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_channel()
    {
        await _fdc3.SubscribeAsync();
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";

        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: originFdc3InstanceId,
            channelType: channelType,
            channelId: channelId), _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: raiseIntentResponse.MessageId!,
             intent: "intentMetadata4",
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.SubscribeAsync();
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var voidResult = "dummy error happened during Promise";

        var target = await MockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "intentMetadata4",
                selected: false,
                context: new Context("context2"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            messageId: raiseIntentResponse.MessageId!,
            intent: "intentMetadata4",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: originFdc3InstanceId,
            voidResult: voidResult), _options);

        var storeResult = await _fdc3.HandleStoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             messageId: raiseIntentResponse.MessageId!,
             intent: "intentMetadata4",
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.HandleGetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(voidResult: voidResult));

        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_no_payload()
    {
        var result = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, null, new());
        result.Should().NotBeNull();
        result!.ReadJson<AddIntentListenerResponse>(_options).Should().BeEquivalentTo(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_missing_id()
    {
        var request = new AddIntentListenerRequest("dummy", Guid.NewGuid().ToString(), SubscribeState.Unsubscribe);
        var result = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, MessageBuffer.Factory.CreateJson(request, _options), new());
        result.Should().NotBeNull();
        result!.ReadJson<AddIntentListenerResponse>(_options).Should().BeEquivalentTo(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes_to_existing_raised_intent()
    {
        await _fdc3.SubscribeAsync();

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: originFdc3InstanceId,
                intent: "intentMetadataCustom",
                selected: false,
                context: new Context("contextCustom"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: "intentMetadataCustom",
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));

        await MockModuleLoader.Object.StopModule(new(origin.InstanceId));
        await MockModuleLoader.Object.StopModule(new(target.InstanceId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes()
    {
        await _fdc3.SubscribeAsync();

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: "intentMetadataCustom",
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                messageId: 1,
                fdc3InstanceId: originFdc3InstanceId,
                intent: "intentMetadataCustom",
                selected: false,
                context: new Context("contextCustom"),
                targetAppIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

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
        await _fdc3.SubscribeAsync();

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await MockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await MockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: "intentMetadataCustom",
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: "intentMetadataCustom",
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Unsubscribe));

        addIntentListenerResult = await _fdc3.HandleAddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
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
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("contextCustom")),//This relates to the appId4 only
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadataCustom", "intentMetadataCustom", new [] { "contextCustom" }),
                            new []
                            {
                                new AppMetadata(appId:"appId4", name: "app4", resultType: null)
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple app by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context2")), //This relates to the appId4, appId5, appId6,
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata4", "displayName4", new [] { "context2" }),
                            new []
                            {
                                new AppMetadata(appId:"appId4", name: "app4", resultType: null),
                                new AppMetadata(appId:"appId5", name: "app5", resultType: "resultType<specified>"),
                                new AppMetadata(appId:"appId6", name: "app6", resultType: "resultType")
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple appIntent by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context9")),//This relates to the wrongappId9 and an another wrongAppId9 with 2 individual IntentMetadata
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata9", "displayName9", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("wrongappId9", name: "app9", resultType: "resultWrongApp"),
                            }),
                        new AppIntent(
                            new IntentMetadata("intentMetadata10", "displayName10", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("appId11", name: "app11", resultType: "channel<specified>"),
                            }),
                        new AppIntent(
                            new IntentMetadata("intentMetadata11", "displayName11", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("appId12", name: "app12", resultType: "resultWrongApp"),
                            })
                    }
                },
                ExpectedAppIntentsCount = 3
            });

            // Returning error no apps found by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("noAppShouldReturn")),// no app should have this context type
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.NoAppsFound
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning one AppIntent with one app by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context2"), //This relates to multiple appId
                    resultType: "resultType<specified>"),
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata4", "displayName4", new [] {"context2", "context5"}), // it should just return appId5
                            new []
                            {
                                new AppMetadata("appId5", name: "app5", resultType: "resultType<specified>")
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple apps by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context2"), //This relates to multiple appId
                    resultType: "resultType"),
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2", "context5" }),
                            new[]
                            {   
                                new AppMetadata(appId:"appId5", name: "app5", resultType: "resultType<specified>"),
                                new AppMetadata(appId:"appId6", name: "app6", resultType: "resultType")
                            }),
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple AppIntent by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context9"), //This relates to multiple appId
                    resultType: "resultWrongApp"),
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata9", "displayName9", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("wrongappId9", name: "app9", resultType: "resultWrongApp"),
                            }),
                        new AppIntent(
                            new IntentMetadata("intentMetadata11", "displayName11", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("appId12", name: "app12", resultType: "resultWrongApp")
                            })
                    }
                },
                ExpectedAppIntentsCount = 2
            });

            // Returning no apps found error by using ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("context9"), //This relates to multiple appId
                    resultType: "noAppShouldReturn"),
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
                Request = new FindIntentsByContextRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    context: new Context("fdc3.nothing"),
                    resultType: "resultWrongApp"),
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            new IntentMetadata("intentMetadata9", "displayName9", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("wrongappId9", name: "app9", resultType: "resultWrongApp"),
                            }),

                        new AppIntent(
                            new IntentMetadata("intentMetadata11", "displayName11", new [] { "context9" }),
                            new []
                            {
                                new AppMetadata("appId12", name: "app12", resultType: "resultWrongApp")
                            }),
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
                    Request = new FindIntentRequest(
                        fdc3InstanceId: Guid.NewGuid().ToString(),
                        intent: "intentMetadata8")
                });

            AddRow(
                new FindIntentTestCase()
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse()
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    Request = new FindIntentRequest(
                        fdc3InstanceId: Guid.NewGuid().ToString(),
                        intent: "intentMetadata2",
                        context: new Context("noAppShouldBeReturned"))
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
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata7",
                    context: new Context("context8"),
                    resultType: "resultType2<specified2>"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadat7", "displayName7", new[] { "context8" }),
                        new[]
                        {
                            new AppMetadata("appId7", name: "app7", resultType: "resultType2<specified2>")
                        })
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata4",
                    context: new Context("context2"),
                    resultType: "resultType"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2", "context5" }),
                        new[]
                        {
                            new AppMetadata("appId5", name: "app5", resultType: "resultType<specified>"),
                            new AppMetadata("appId6", name: "app6", resultType: "resultType"),

                        })
                },
                ExpectedAppCount = 2
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata7",
                    resultType: "resultType2<specified2>"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadat7", "displayName7", new[] { "context8" }),
                        new[]
                        {
                            new AppMetadata("appId7", name: "app7", resultType: "resultType2<specified2>")
                        })
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata4",
                    resultType: "resultType"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2" }),
                        new[]
                        {
                            new AppMetadata("appId5", name: "app5", resultType: "resultType<specified>"),
                            new AppMetadata("appId6", name: "app6", resultType: "resultType"),
                        })
                },
                ExpectedAppCount = 2
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata1",
                    context: new Context("context1")),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata1", "displayName1", new[] { "context1" }),
                        new[]
                        {
                            new AppMetadata("appId1", name: "app1", resultType: null)
                        })
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata4",
                    context: new Context("context2")),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2" }),
                        new[]
                        {
                            new AppMetadata(appId:"appId4", name: "app4", resultType: null),
                            new AppMetadata(appId:"appId5", name: "app5", resultType: "resultType<specified>"),
                            new AppMetadata(appId:"appId6", name: "app6", resultType: "resultType")
                        })
                },
                ExpectedAppCount = 3
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata2"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata2", "displayName2", new[] { "dummyContext" }),
                        new[]
                        {
                            new AppMetadata("appId2", name: "app2", resultType: null)
                        })
                },
                ExpectedAppCount = 1
            });

            AddRow(new FindIntentTestCase()
            {
                Request = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: "intentMetadata4"),
                ExpectedResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        new IntentMetadata("intentMetadata4", "displayName4", new[] { "context2" }),
                        new[]
                        {
                                new AppMetadata(appId:"appId4", name: "app4", resultType: null),
                                new AppMetadata(appId:"appId5", name: "app5", resultType: "resultType<specified>"),
                                new AppMetadata(appId:"appId6", name: "app6", resultType: "resultType")
                        })
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
