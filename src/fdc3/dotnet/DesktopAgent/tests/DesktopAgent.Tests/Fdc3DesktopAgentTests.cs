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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3DesktopAgentTests
{
    private static readonly Mock<IMessageRouter> MockMessageRouter = new();
    private static readonly MockAppDirectory MockAppDirectory = new();
    private static readonly MockModuleLoader MockModuleLoader = new();

    private Fdc3DesktopAgent _fdc3 = new(
        MockAppDirectory,
        MockModuleLoader,
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
            new IAppIdentifierJsonConverter(), 
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
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task RaiseIntent_fails_by_request_delivery_error()
    {
        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            null,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_Context()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3);
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var request = new RaiseIntentRequest(
            raiseIntentMessageId: 1,
            fdc3InstanceId: Guid.NewGuid().ToString(),
            intent: intentMetadata.Name,
            selected: false,
            context: new Context(intentMetadata.Contexts.First()));

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_saves_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.SubscribeAsync();
        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId4", 
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, targetFdc3InstanceId) }));

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var request = new RaiseIntentRequest(
            raiseIntentMessageId: 1,
            fdc3InstanceId: Guid.NewGuid().ToString(),
            intent: intentMetadata.Name,
            selected: false,
            context: new Context(intentMetadata.Contexts.First()),
            appIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId));

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        MockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.AddIntentListener, It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        MockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.RaiseIntentResolution(intentMetadata.Name, targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.SubscribeAsync();

        var originFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var originatingApp = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId1",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, originFdc3InstanceId) }));


        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId4",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, targetFdc3InstanceId) }));

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: intentMetadata.Name,
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest(
            raiseIntentMessageId: 1,
            fdc3InstanceId: originFdc3InstanceId,
            intent: intentMetadata.Name,
            selected: false,
            context: new Context(intentMetadata.Contexts.First()),
            appIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId));

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution(intentMetadata.Name, targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        MockModuleLoader.StopAllModules();
    }


    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context()
    {
        var apps = await MockAppDirectory.GetApps();
        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: instanceId,
                intent: intentMetadata.Name, //intentMetadata4
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                null,
                null));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType),
                new(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.First().Value.ResultType),
                new(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
            });
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context_if_fdc3_nothing()
    {
        var apps = await MockAppDirectory.GetApps();
        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: instanceId,
                intent: intentMetadata.Name, //intentMetadata4
                selected: false,
                context: new Context("fdc3.nothing"),
                null,
                null));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType),
                new(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.First().Value.ResultType),
                new(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
            });
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "testIntent",
                selected: false,
                context: new Context("contextType"),
                appIdentifier: new AppIdentifier("noAppShouldReturn"),
                null));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Context()
    {
        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First();

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context("noAppShouldReturn"),
                null,
                null));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //context2

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "noAppShouldReturn",
                selected: false,
                context: new Context(intentMetadata.Contexts!.First()),
                null,
                null));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_multiple_IAppIntents_found()
    {
        var app = await MockAppDirectory.GetApp("appId7");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1); //intentMetadata8

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts!.First()),
                null,
                null));

        var result = await _fdc3.RaiseIntent(
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
                raiseIntentMessageId: 1,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: "someIntent",
                selected: false,
                context: null,
                null,
                error: "Some weird error"));

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be("Some weird error");
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.StoreIntentResult("dummy", null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request_contains_no_information()
    {
        var app = await MockAppDirectory.GetApp("appId4"); 
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            intent: "dummy",
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: null,
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var result = await _fdc3.StoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_previosly_no_saved_raiseIntent_could_handle()
    {
        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var storeIntentRequest = new StoreIntentResultRequest(
            intent: "dummy",
            originFdc3InstanceId: originFdc3InstanceId,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var action = async() => await _fdc3.StoreIntentResult("dummy", MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        
        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>();
    }


    [Fact]
    public async Task StoreIntentResult_succeeds_with_channel()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: "dummyChannelId",
            channelType: ChannelType.User,
            null);

        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_context()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: null,
            channelType: null,
            context: new Context("test"));

        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_errorResult()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: Guid.NewGuid().ToString(),
            channelId: null,
            channelType: null,
            context: null,
            errorResult: "dummy error happened during Promise");

        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task GetIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_Source_is_null()
    {
        //Version should be the Intent's schema version
        var getIntentResultRequest = new GetIntentResultRequest(
             intent: "dummy", 
             targetAppIdentifier: new AppIdentifier("dummy", Guid.NewGuid().ToString()),
             version: "1.0"); 

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_InstanceId_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest(
             intent: "dummy",
             targetAppIdentifier: new AppIdentifier("dummy"),
             version: "1.0");

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_no_intent_found()
    {
        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: targetFdc3InstanceId,
            context: context), _options);

        var storeResult = await _fdc3.StoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             intent: "dummy",
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!),
             version: "1.0");

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_context()
    {
        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var app = await MockAppDirectory.GetApp("appId4"); 
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: targetFdc3InstanceId,
            context: context), _options);

        var storeResult = await _fdc3.StoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             intent: intentMetadata.Name,
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(context: context));

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_channel()
    {
        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: targetFdc3InstanceId,
            channelType: channelType,
            channelId: channelId), _options);

        var storeResult = await _fdc3.StoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             intent: intentMetadata.Name,
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_errorResult()
    {
        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        var errorResult = "dummy error happened during Promise";

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: int.MaxValue,
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier(app.AppId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResponse.Should().NotBeNull();
        raiseIntentResponse!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = MessageBuffer.Factory.CreateJson(new StoreIntentResultRequest(
            intent: intentMetadata.Name,
            originFdc3InstanceId: raiseIntentResponse.AppMetadata!.First().InstanceId!,
            targetFdc3InstanceId: targetFdc3InstanceId,
            errorResult: errorResult), _options);

        var storeResult = await _fdc3.StoreIntentResult(
            Fdc3Topic.SendIntentResult,
            storeIntentRequest,
            new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest(
             intent: intentMetadata.Name,
             targetAppIdentifier: new AppIdentifier("appId1", raiseIntentResponse.AppMetadata!.First().InstanceId!));

        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(errorResult: errorResult));

        MockModuleLoader.StopAllModules();
    }

    [Fact]
    public async Task AddIntentListener_fails_due_no_payload()
    {
        var result = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, null, new());
        result.Should().NotBeNull();
        result!.ReadJson<AddIntentListenerResponse>(_options).Should().BeEquivalentTo(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_missing_id()
    {
        var request = new AddIntentListenerRequest("dummy", Guid.NewGuid().ToString(), SubscribeState.Unsubscribe);
        var result = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, MessageBuffer.Factory.CreateJson(request, _options), new());
        result.Should().NotBeNull();
        result!.ReadJson<AddIntentListenerResponse>(_options).Should().BeEquivalentTo(AddIntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes_to_existing_raised_intent()
    {
        await _fdc3.SubscribeAsync();

        var originFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var originatingApp = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId1",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, originFdc3InstanceId) }));


        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId4",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, targetFdc3InstanceId) }));

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: originFdc3InstanceId,
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: intentMetadata.Name,
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution(intentMetadata.Name, targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddIntentListener_subscribes()
    {
        await _fdc3.SubscribeAsync();

        var originFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var originatingApp = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId1",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, originFdc3InstanceId) }));


        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId4",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, targetFdc3InstanceId) }));

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: intentMetadata.Name,
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest(
                raiseIntentMessageId: 1,
                fdc3InstanceId: originFdc3InstanceId,
                intent: intentMetadata.Name,
                selected: false,
                context: new Context(intentMetadata.Contexts.First()),
                appIdentifier: new AppIdentifier("appId4", targetFdc3InstanceId)));

        var raiseIntentResult = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First()!.AppId.Should().Be(app.AppId);

        MockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution(intentMetadata.Name, targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddIntentListener_unsubscribes()
    {
        await _fdc3.SubscribeAsync();
        var originFdc3InstanceId = Guid.NewGuid().ToString();

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var originatingApp = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId1",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, originFdc3InstanceId) }));


        var targetFdc3InstanceId = Guid.NewGuid().ToString();
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await MockModuleLoader.StartModule(
            new StartRequest(
                "appId4",
                new KeyValuePair<string, string>[] { new KeyValuePair<string, string>(Fdc3StartupProperties.Fdc3InstanceId, targetFdc3InstanceId) }));

        var app = await MockAppDirectory.GetApp("appId4");
        var intentMetadata = app!.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: intentMetadata.Name,
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Subscribe));

        var addIntentListenerResult = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new AddIntentListenerRequest(
                intent: intentMetadata.Name,
                fdc3InstanceId: targetFdc3InstanceId,
                state: SubscribeState.Unsubscribe));

        addIntentListenerResult = await _fdc3.AddIntentListener(Fdc3Topic.AddIntentListener, addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        addIntentListnerResponse = addIntentListenerResult!.ReadJson<AddIntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeFalse();
        addIntentListnerResponse!.Error.Should().BeNull();
    }

    private MessageBuffer FindTestChannel => MessageBuffer.Factory.CreateJson(new FindChannelRequest() { ChannelId = "testChannel", ChannelType = ChannelType.User });


    [Theory]
    [ClassData(typeof(FindIntentTheoryData))]
    public async Task FindIntent_edge_case_tests(FindIntentTestCase testCase)
    {
        var request = MessageBuffer.Factory.CreateJson(testCase.Request, _options);
        var result = await _fdc3.FindIntent(Fdc3Topic.FindIntent, request, new MessageContext());

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
        var result = await _fdc3.FindIntentsByContext(Fdc3Topic.FindIntentsByContext, request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppIntentsCount > 0)
        {
            result!.ReadJson<FindIntentsByContextResponse>(_options)!.AppIntents!.Should().HaveCount(testCase.ExpectedAppIntentsCount);
        }

        result!.ReadJson<FindIntentsByContextResponse>(_options)!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    public class FindIntentsByContextTheoryData : TheoryData
    {
        private readonly IEnumerable<Fdc3App> _apps;
        public FindIntentsByContextTheoryData()
        {
            _apps = MockAppDirectory.Apps;

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
                            _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1),
                            new []
                            {
                                new AppMetadata(appId: _apps.ElementAt(3).AppId, name: _apps.ElementAt(3).Name, resultType: _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType)
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
                            _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.First(), //it should be the same for all app - the IntentMetadata
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(3).AppId, name: _apps.ElementAt(3).Name, resultType: _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.First().ResultType),
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType),
                                new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType)
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
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                                new AppMetadata(_apps.ElementAt(10).AppId, name: _apps.ElementAt(10).Name, resultType: _apps.ElementAt(10)!.Interop!.Intents!.ListensFor!.Values.First().ResultType) //appId11
                            })
                    }
                },
                ExpectedAppIntentsCount = 2
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
                            _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First(), // it should just return appId5
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType)
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
                            _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                                new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType) //appId6
                            })
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
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
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
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //wrongAppId9
                            }),
                    }
                },
                ExpectedAppIntentsCount = 3
            });
        }
    }

    public class FindIntentsByContextTestCase
    {
        public FindIntentsByContextRequest Request { get; set; }
        public FindIntentsByContextResponse ExpectedResponse { get; set; }
        public int ExpectedAppIntentsCount { get; set; }
    }

    private class FindIntentTheoryData : TheoryData
    {
        private readonly IEnumerable<Fdc3App> _apps;
        public FindIntentTheoryData()
        {
            _apps = MockAppDirectory.Apps;
            AddRow(GenerateTestCase(FindIntentTestCaseType.Context, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Context, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Intent, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Intent, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ResultType, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ResultType, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ContextResultType, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ContextResultType, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.NoAppsFound));
            AddRow(GenerateTestCase(FindIntentTestCaseType.RequestError));
            AddRow(GenerateTestCase(FindIntentTestCaseType.MultipleAppIntents));
        }

        private FindIntentTestCase GenerateTestCase(
            FindIntentTestCaseType findIntentTestCaseType,
            bool multipleResult = false)
        {
            return findIntentTestCaseType switch
            {
                FindIntentTestCaseType.Intent => SelectTestCaseForIntent(multipleResult),
                FindIntentTestCaseType.Context => SelectTestCaseForContext(multipleResult),
                FindIntentTestCaseType.ResultType => SelectTestCaseForResultType(multipleResult),
                FindIntentTestCaseType.ContextResultType => SelectTestCaseForContextAndResultType(multipleResult),
                FindIntentTestCaseType.RequestError => SelectTestCaseForRequestError(),
                FindIntentTestCaseType.NoAppsFound => SelectTestCaseForNoAppsFoundError(),
                FindIntentTestCaseType.MultipleAppIntents => SelectTestCaseForMultipleAppIntentsError(),
                _ => throw new NotImplementedException(),
            };
        }
        
        private FindIntentTestCase SelectTestCaseForMultipleAppIntentsError()
        {
            //As per the documentation : https://github.com/morganstanley/fdc3-dotnet/blob/main/src/Fdc3/IIntentMetadata.cs
            //name is unique for the intents, so it should be unique for every app, or the app should have the same intentMetadata?
            //if so we should return multiple appIntents and do not return error message for the client.
            //We have setup a test case for wrongappId9 which contains wrongly setted up intentMetadata.
            var app = _apps.ElementAt(6); //appId7
            var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1); //intentMetadata8

            var findIntentRequest = new FindIntentRequest(
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Key);

            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = findIntentRequest
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForNoAppsFoundError()
        {
            var app = _apps.ElementAt(1);
            var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

            var findIntentRequest = new FindIntentRequest(
                fdc3InstanceId: Guid.NewGuid().ToString(),
                intent: intentMetadata.Key,
                context: new Context("noAppShouldBeReturned"));

            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.NoAppsFound
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = findIntentRequest
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForRequestError()
        {
            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = null
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForContextAndResultType(bool multipleResult)
        {
            FindIntentTestCase result;

            if (!multipleResult)
            {
                var app = _apps.ElementAt(6); //appId7
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key, //"intentMetadata7"
                    context: new Context(intentMetadata.Value.Contexts.First()), //"context8"
                    resultType: intentMetadata.Value.ResultType);//"resultType2<specified2>"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(5); //appId6
                var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1);

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key, //intentMetadata4
                    context: new Context(intentMetadata.Value.Contexts.First()), //"context2"
                    resultType: intentMetadata.Value.ResultType);//"resultType"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),

                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 2
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForResultType(bool multipleResult)
        {
            FindIntentTestCase result;

            if (!multipleResult)
            {
                var app = _apps.ElementAt(6); //appId7
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key,
                    resultType: intentMetadata.Value.ResultType); //"resultType2<specified2>"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        intentMetadata.Value,
                        new[]
                        {
                            new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                        })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(5); //appId6
                var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1);

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key, //intentMetadata4
                    resultType: intentMetadata.Value.ResultType); //"resultType"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),

                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 2
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForContext(bool multipleResult)
        {
            FindIntentTestCase result;
            if (!multipleResult)
            {
                var app = _apps.ElementAt(1);
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key,
                    context: new Context(intentMetadata.Value.Contexts.First())); //"context1"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(3);
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key,
                    context: new Context(intentMetadata.Value.Contexts.First()));//"context2"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5).Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //appId6
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 3
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForIntent(bool multipleResult)
        {
            FindIntentTestCase result;
            if (!multipleResult)
            {
                var app = _apps.ElementAt(1); //appId2
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key);//"intentMetadata2"

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(3); //appId4
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest(
                    fdc3InstanceId: Guid.NewGuid().ToString(),
                    intent: intentMetadata.Key);

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5).Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //appId6
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 3
                };
            }

            return result!;
        }

        private enum FindIntentTestCaseType
        {
            Context,
            ResultType,
            Intent,
            ContextResultType,
            RequestError,
            NoAppsFound,
            MultipleAppIntents,
        }
    }

    public class FindIntentTestCase
    {
        public FindIntentRequest Request { get; set; }
        public FindIntentResponse ExpectedResponse { get; set; }
        public int ExpectedAppCount { get; set; }
    }
}
