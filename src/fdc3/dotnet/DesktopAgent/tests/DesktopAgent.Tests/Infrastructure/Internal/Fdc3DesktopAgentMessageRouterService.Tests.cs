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

using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using Finos.Fdc3;
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class Fdc3DesktopAgentMessageRouterServiceTests
{
    private readonly Mock<IMessageRouter> _mockMessageRouter = new();
    private readonly MockModuleLoader _mockModuleLoader = new();
    private readonly IAppDirectory _appDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions()
        {
            Source = new Uri($"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json")
        });

    private readonly Fdc3DesktopAgentMessageRouterService _fdc3;
    private const string TestChannel = "testChannel";

    public Fdc3DesktopAgentMessageRouterServiceTests()
    {
        _fdc3 = new(
            _mockMessageRouter.Object,
            new Fdc3DesktopAgent(_appDirectory, _mockModuleLoader.Object, new Fdc3DesktopAgentOptions(), NullLoggerFactory.Instance),
            new Fdc3DesktopAgentOptions(),
            NullLoggerFactory.Instance);
    }

    [Fact]
    public async void UserChannelAddedCanBeFound()
    {
        await _fdc3.HandleAddUserChannel(TestChannel);

        var result = await _fdc3.HandleFindChannel(FindTestChannel, new MessageContext());

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.AppMetadata!.First()!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task RaiseIntent_fails_by_request_delivery_error()
    {
        var result = await _fdc3.HandleRaiseIntent(null, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
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

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());

        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.AppMetadata!.First()!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_saves_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
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

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());

        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.AddIntentListener, It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockMessageRouter.Verify(
            _ => _.InvokeAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<InvokeOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();

        addIntentListenerResult!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest()
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        result!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    //TODO: Right now we are returning just one element, without the possibility of selecting via ResolverUI.
    [Fact]
    public async Task RaiseIntent_returns_first_app_by_Context()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = instanceId,
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2")
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First().AppId.Should().Be("appId4");
    }

    //TODO: Right now we are returning just one element, without the possibility of selecting via ResolverUI.
    [Fact]
    public async Task RaiseIntent_returns_first_app_by_Context_if_fdc3_nothing()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = instanceId,
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context(ContextTypes.Nothing)
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();

        result!.AppMetadata.Should().HaveCount(1);
        result!.AppMetadata!.First().AppId.Should().Be("appId4");
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "testIntent",
                Selected = false,
                Context = new Context("contextType"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "noAppShouldReturn" }
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Context()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("noAppShouldReturn")
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "noAppShouldReturn",
                Selected = false,
                Context = new Context("context2")
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_request_specifies_error()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "someIntent",
                Selected = false,
                Error = "Some weird error"
            };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());

        result!.Error.Should().Be("Some weird error");
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.HandleStoreIntentResult(null, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request_contains_no_information()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "dummy",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = null,
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User,
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
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

        var action = async () => await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>();
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.Error.Should().BeNull();
        raiseIntentResult.AppMetadata.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_context()
    {
        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = true,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4" }
            };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = new Context("test")
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task StoreIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = null,
            VoidResult = true
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.HandleGetIntentResult(null, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
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

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
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

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_no_intent_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId! },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_context()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(context: context));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";
        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest =new RaiseIntentRequest()
            {
                MessageId = int.MaxValue,
                Fdc3InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata4",
                Selected = false,
                Context = new Context("context2"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            ChannelType = channelType,
            ChannelId = channelId
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());

        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));
    }

    [Fact]
    public async Task GetIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();

        var target = await _mockModuleLoader.Object.StartModule(new("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);

        var storeIntentRequest = new StoreIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            VoidResult = true
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest()
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier() { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(voidResult: true));
    }

    [Fact]
    public async Task AddIntentListener_fails_due_no_payload()
    {
        var result = await _fdc3.HandleAddIntentListener(null, new());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
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
        var result = await _fdc3.HandleAddIntentListener(request, new());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListener_subscribes_to_existing_raised_intent()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
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

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResult!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        var addIntentListenerRequest = new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddIntentListener_subscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest()
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Selected = false,
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier() { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult!.AppMetadata!.First()!.AppId.Should().Be("appId4");
        raiseIntentResult!.AppMetadata!.First()!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId), It.IsAny<MessageBuffer>(), It.IsAny<PublishOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task AddIntentListener_unsubscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            };

        var addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        addIntentListenerRequest = new IntentListenerRequest()
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Unsubscribe
            };

        addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeFalse();
        addIntentListenerResult!.Error.Should().BeNull();
    }

    private FindChannelRequest FindTestChannel => new FindChannelRequest() { ChannelId = "testChannel", ChannelType = ChannelType.User };


    [Theory]
    [ClassData(typeof(FindIntentTheoryData))]
    public async Task FindIntent_edge_case_tests(FindIntentTestCase testCase)
    {
        var request = testCase.Request;
        var result = await _fdc3.HandleFindIntent(request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppCount > 0)
        {
            result!.AppIntent!.Apps.Should().HaveCount(testCase.ExpectedAppCount);
        }

        result!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    [Theory]
    [ClassData(typeof(FindIntentsByContextTheoryData))]
    public async Task FindIntentsByContext_edge_case_tests(FindIntentsByContextTestCase testCase)
    {
        var request = testCase.Request;
        var result = await _fdc3.HandleFindIntentsByContext(request, new MessageContext());

        result.Should().NotBeNull();

        result!.Should().BeEquivalentTo(testCase.ExpectedResponse);
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
                }
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
                }
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
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"},
                            }
                        },
                    }
                }
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
                }
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
                }
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
                }
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
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata10", DisplayName = "displayName10" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp" }
                            }
                        }
                    }
                }
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
                }
            });

            // Returning intent delivery error
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = null,
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.IntentDeliveryFailed
                }
            });

            // Returning all the apps that are using the ResultType by adding fdc3.nothing.
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Fdc3InstanceId = Guid.NewGuid().ToString(),
                    Context = new Context(ContextTypes.Nothing),
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
                            Intent = new Protocol.IntentMetadata () { Name = "intentMetadata10", DisplayName = "displayName10" },
                            Apps = new []
                            {
                                new AppMetadata() { AppId = "appId12", Name = "app12", ResultType = "resultWrongApp" }
                            }
                        },
                    }
                }
            });
        }
    }

    public class FindIntentsByContextTestCase
    {
        internal FindIntentsByContextRequest Request { get; set; }
        internal FindIntentsByContextResponse ExpectedResponse { get; set; }
    }

    private class FindIntentTheoryData : TheoryData
    {
        public FindIntentTheoryData()
        {
            AddRow(
                new FindIntentTestCase()
                {
                    ExpectedAppCount = 2,
                    ExpectedResponse = FindIntentResponse.Success(
                        new AppIntent
                        {
                            Intent = new Protocol.IntentMetadata() { Name = "intentMetadata8", DisplayName = "displayName8" },
                            Apps = new[]
                            {
                                new AppMetadata(){ AppId = "appId7", Name = "app7", ResultType = "resultType2<specified>" },
                                new AppMetadata(){ AppId = "appId8", Name = "app8", ResultType = "resultType2<specified>" }
                            }
                        }),
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
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata7", DisplayName = "displayName7" },
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
                        Intent = new Protocol.IntentMetadata() { Name = "intentMetadata7", DisplayName = "displayName7" },
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
