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
using Finos.Fdc3.AppDirectory;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using IntentMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.IntentMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class Fdc3DesktopAgentMessageRouterServiceTests : IAsyncLifetime
{
    private const string TestChannel = "fdc3.channel.1";

    private readonly IAppDirectory _appDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions
        {
            Source = new Uri($"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json")
        });

    private readonly Fdc3DesktopAgentMessageRouterService _fdc3;
    private readonly Mock<IMessageRouter> _mockMessageRouter = new();
    private readonly Mock<IResolverUICommunicator> _mockResolverUICommunicator = new();
    private readonly MockModuleLoader _mockModuleLoader = new();
    private readonly ConcurrentDictionary<Guid, IModuleInstance> _modules = new();
    private IDisposable? _disposable;

    public Fdc3DesktopAgentMessageRouterServiceTests()
    {
        var options = new Fdc3DesktopAgentOptions()
        {
            IntentResultTimeout = TimeSpan.FromMilliseconds(100),
            ListenerRegistrationTimeout = TimeSpan.FromMilliseconds(100)
        };

        _fdc3 = new Fdc3DesktopAgentMessageRouterService(
            _mockMessageRouter.Object,
            new Fdc3DesktopAgent(
                _appDirectory,
                _mockModuleLoader.Object,
                options,
                _mockResolverUICommunicator.Object,
                new UserChannelSetReader(options),
                NullLoggerFactory.Instance),
            new Fdc3DesktopAgentOptions(),
            NullLoggerFactory.Instance);
    }

    private FindChannelRequest FindTestChannel => new() {ChannelId = "fdc3.channel.1", ChannelType = ChannelType.User};

    public async Task InitializeAsync()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        _disposable = _mockModuleLoader.Object.LifetimeEvents.Subscribe(x =>
        {
            switch (x.EventType)
            {
                case LifetimeEventType.Started:
                    _modules.TryAdd(x.Instance.InstanceId, x.Instance);
                    break;

                case LifetimeEventType.Stopped:
                    _modules.TryRemove(x.Instance.InstanceId, out _);
                    break;
            }
        });
    }

    public async Task DisposeAsync()
    {
        await _fdc3.StopAsync(CancellationToken.None);

        foreach (var module in _modules)
        {
            await _mockModuleLoader.Object.StopModule(new(module.Key));
        }

        _disposable?.Dispose();
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
    public async Task HandleRaiseIntent_returns_IntentDeliveryFailed_error_as_no_intent_listener_is_registered_after_starting_an_app()
    {
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4" }
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());

        _mockModuleLoader.Verify(_ => _.StartModule(It.IsAny<StartRequest>()));
        result.Should().NotBeNull();
        result!.Error.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.IntentDeliveryFailed); //Due no intent handler was registered by the started app.
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_by_request_delivery_error_as_request_null()
    {
        var result = await _fdc3.HandleRaiseIntent(request: null, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task HandleRaiseIntent_returns_IntentDeliveryFailed_error_as_no_intent_listener_is_registered()
    {
        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom")
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();

        _mockModuleLoader.Verify(_ => _.StartModule(It.IsAny<StartRequest>()));
        result.Should().NotBeNull();
        result!.Error.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.IntentDeliveryFailed); //Due no intent handler was registered by the started app.
    }

    [Fact]
    public async Task
        HandleRaiseIntent_returns_one_app_by_AppIdentifier_and_saves_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();
        result!.AppMetadata.Should().NotBeNull();
        result!.AppMetadata!.AppId.Should().Be("appId4");
        result!.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.InvokeAsync(
                Fdc3Topic.AddIntentListener,
                It.IsAny<MessageBuffer>(),
                It.IsAny<InvokeOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mockMessageRouter.Verify(
            _ => _.InvokeAsync(
                Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<InvokeOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task
        HandleRaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();
        result!.AppMetadata.Should().NotBeNull();
        result!.AppMetadata!.AppId.Should().Be("appId4");
        result!.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleRaiseIntent_calls_ResolverUI_by_Context_filter()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = instanceId,
            Intent = "intentMetadata4",
            Context = new Context("context2")
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        _mockResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleRaiseIntent_calls_ResolverUI_by_Context_filter_if_fdc3_nothing()
    {
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = instanceId,
            Intent = "intentMetadata4",
            Context = new Context(ContextTypes.Nothing)
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        _mockResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "testIntent",
            Context = new Context("contextType"),
            TargetAppIdentifier = new AppIdentifier { AppId = "noAppShouldReturn" }
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.TargetAppUnavailable);
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_Context()
    {
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("noAppShouldReturn")
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "noAppShouldReturn",
            Context = new Context("context2")
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task HandleStoreIntentResult_fails_due_the_request_is_null()
    {
        var result = await _fdc3.HandleStoreIntentResult(request: null, new MessageContext());
        result.Should().NotBeNull();
        result!.Should()
            .BeEquivalentTo(new StoreIntentResultResponse { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task HandleStoreIntentResult_fails_as_no_saved_raiseIntent_could_handle()
    {
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            OriginFdc3InstanceId = originFdc3InstanceId,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());

        result.Should().BeEquivalentTo(StoreIntentResultResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task HandleStoreIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.Error.Should().BeNull();
        raiseIntentResult.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = "dummyChannelId",
            ChannelType = ChannelType.User
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse { Stored = true });
    }

    [Fact]
    public async Task HandleStoreIntentResult_succeeds_with_context()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.Error.Should().BeNull();
        raiseIntentResult.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = new Context("test")
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse { Stored = true });
    }

    [Fact]
    public async Task HandleStoreIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = null,
            VoidResult = true
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse { Stored = true });
    }

    [Fact]
    public async Task HandleGetIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.HandleGetIntentResult(request: null, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task HandleGetIntentResult_fails_intent_not_found()
    {
        //Version should be the Intent's schema version
        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier { AppId = "dummy", InstanceId = Guid.NewGuid().ToString() },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task HandleGetIntentResult_fails_due_InstanceId_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier { AppId = "dummy" },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task HandleGetIntentResult_fails_due_no_intent_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var context = new Context("test");

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier
            { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.InstanceId! },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_context()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var context = new Context("test");
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = context
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier
            { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(context: context));
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_channel()
    {
        await _fdc3.StartAsync(CancellationToken.None);
        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            ChannelType = channelType,
            ChannelId = channelId
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier
            { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should()
            .BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_voidResult()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        var originFdc3InstanceId = Guid.NewGuid().ToString();
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadata4",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = int.MaxValue,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = "intentMetadata4",
            Context = new Context("context2"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            VoidResult = true
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadata4",
            TargetAppIdentifier = new AppIdentifier
            { AppId = "appId1", InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(voidResult: true));
    }

    [Fact]
    public async Task HandleAddIntentListener_fails_due_no_payload()
    {
        var result = await _fdc3.HandleAddIntentListener(request: null, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task HandleAddIntentListener_fails_due_missing_id()
    {
        var request = new IntentListenerRequest
        {
            Intent = "dummy",
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            State = SubscribeState.Unsubscribe
        };

        var result = await _fdc3.HandleAddIntentListener(request, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task HandleAddIntentListener_subscribes_to_existing_raised_intent()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult.AppMetadata!.AppId.Should().Be("appId4");
        raiseIntentResult.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAddIntentListener_subscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult!.AppMetadata!.AppId.Should().Be("appId4");
        raiseIntentResult!.AppMetadata.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution("intentMetadataCustom", targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAddIntentListener_unsubscribes()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult =
            await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Unsubscribe
        };

        addIntentListenerResult = await _fdc3.HandleAddIntentListener(addIntentListenerRequest, new MessageContext());
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeFalse();
        addIntentListenerResult.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleCreateAppChannel_fails_due_request_is_null()
    {
        CreateAppChannelRequest? request = null;

        var result = await _fdc3.HandleCreateAppChannel(request, new MessageContext());

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task HandleCreateAppChannel_returns_successful_response()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new CreateAppChannelRequest
        {
            ChannelId = "my.channel",
            InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleCreateAppChannel(request, new MessageContext());

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Created());
    }

    [Fact]
    public async Task HandleGetUserChannels_returns_payload_null_error()
    {
        GetUserChannelsRequest? request = null;
        var result = await _fdc3.HandleGetUserChannels(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task HandleGetUserChannels_returns_missing_id_error()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = "NotValidId"
        };

        var result = await _fdc3.HandleGetUserChannels(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task HandleGetUserChannels_returns_access_denied_error()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _fdc3.HandleGetUserChannels(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(ChannelError.AccessDenied));
    }

    [Fact]
    public async Task HandleGetUserChannels_succeeds()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetUserChannelsRequest
        {
            InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleGetUserChannels(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Success(new List<ChannelItem> 
        {
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
    public async Task HandleJoinUserChannel_returns_missing_Id_error_as_instance_id_not_found()
    {
        var request = new JoinUserChannelRequest
        {
            ChannelId = "fdc3.channel.1",
            InstanceId = Guid.NewGuid().ToString()
        };
        var result = await _fdc3.HandleJoinUserChannel(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task HandleJoinUserChannel_returns_no_channel_found_error_as_channel_id_not_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new JoinUserChannelRequest
        {
            ChannelId = "fdc3.channel.dummy",
            InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleJoinUserChannel(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(ChannelError.NoChannelFound));
    }

    [Fact]
    public async Task HandleJoinUserChannel_succeeds()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new JoinUserChannelRequest
        {
            ChannelId = "fdc3.channel.1",
            InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleJoinUserChannel(request, new());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Joined(new DisplayMetadata()
        {
            Color = "red",
            Glyph = "1",
            Name = "Channel 1"
        }));
    }

    [Fact]
    public async Task HandleGetInfo_fails_as_no_payload_received()
    {
        GetInfoRequest? request = null;

        var result = await _fdc3.HandleGetInfo(request, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleGetInfo_fails_as_no_instanceId_received()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = null
            }
        };

        var result = await _fdc3.HandleGetInfo(request, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleGetInfo_fails_as_not_valid_instanceId_received()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = "NotExistentNotParsableGuidId"
            }
        };

        var result = await _fdc3.HandleGetInfo(request, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleGetInfo_fails_as_instanceId_missing_from_running_modules()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = Guid.NewGuid().ToString(),
            }
        };

        var result = await _fdc3.HandleGetInfo(request, new MessageContext());

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleGetInfo_succeeds()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = originFdc3InstanceId,
            }
        };

        var result = await _fdc3.HandleGetInfo(request, new MessageContext());

        result.Should().NotBeNull();
        result!.ImplementationMetadata.Should().NotBeNull();
        result.ImplementationMetadata
            .Should()
            .BeEquivalentTo(new ImplementationMetadata()
            {
                AppMetadata = new AppMetadata
                {
                    AppId = "appId1",
                    InstanceId = originFdc3InstanceId,
                    Description = null,
                    Icons = Enumerable.Empty<Icon>(),
                    Name = "app1",
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
    public async Task HandleFindInstances_returns_PayloadNull_error_as_no_request()
    {
        FindInstancesRequest? request = null;

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleFindInstances_returns_MissingId_as_invalid_id()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = "notValidInstanceId",
        };

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleFindInstances_returns_MissingId_error_as_no_instance_found_which_is_contained_by_the_container()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleFindInstances_returns_NoAppsFound_error_as_no_appId_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "noAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task HandleFindInstances_succeeds_with_one_app()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Instances.Should().HaveCount(1);
        result.Instances!.ElementAt(0).InstanceId.Should().Be(originFdc3InstanceId);
    }

    [Fact]
    public async Task HandleFindInstances_succeeds_with_empty_array()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId2",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var result = await _fdc3.HandleFindInstances(request, null);

        result.Should().NotBeNull();
        result!.Instances.Should().HaveCount(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_PayLoadNull_error_as_request_null()
    {
        GetAppMetadataRequest? request = null;

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_MissingId_error_as_initiator_id_not_found()
    {
        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString(),
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_MissingId_error_as_the_searched_instanceId_not_valid()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = "notValidInstanceId"
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Should().NotBeNull();
        result.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_TargetInstanceUnavailable_error_as_the_searched_instanceId_not_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = Guid.NewGuid().ToString()
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.TargetInstanceUnavailable);
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_AppMetadata_based_on_instanceId()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = originFdc3InstanceId
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

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
    public async Task HandleGetAppMetadata_returns_TargetAppUnavailable_error_as_the_searched_appId_not_found()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "notExistentAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Error.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.TargetAppUnavailable);
    }

    [Fact]
    public async Task HandleGetAppMetadata_returns_AppMetadata_based_on_appId()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var result = await _fdc3.HandleGetAppMetadata(request, null);

        result.Error.Should().BeNull();
        result.AppMetadata.Should().BeEquivalentTo(
            new AppMetadata()
            {
                AppId = "appId1",
                Name = "app1"
            });
    }

    [Fact]
    public async Task HandleAddContextListener_returns_payload_null_error()
    {
        AddContextListenerRequest? request = null;

        var response = await _fdc3.HandleAddContextListener(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleAddContextListener_returns_missing_id_error()
    {
        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = "dummyId",
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var response = await _fdc3.HandleAddContextListener(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleAddContextListener_successfully_registers_context_listener()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var response = await _fdc3.HandleAddContextListener(request, new());

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRemoveContextListener_returns_payload_null_error()
    {
        RemoveContextListenerRequest? request = null;

        var response = await _fdc3.HandleRemoveContextListener(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleRemoveContextListener_returns_missing_id_error()
    {
        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = "dummyId",
            ListenerId = Guid.NewGuid().ToString(),
        };

        var response = await _fdc3.HandleRemoveContextListener(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleRemoveContextListener_returns_listener_not_found_error()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = "fdc3.instrument"
        };

        var addContextListenerResponse = await _fdc3.HandleAddContextListener(addContextListenerRequest, new());
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var response = await _fdc3.HandleRemoveContextListener(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.ListenerNotFound);
    }

    [Fact]
    public async Task HandleRemoveContextListener_successfully_removes_context_listener()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = null
        };

        var addContextListenerResponse = await _fdc3.HandleAddContextListener(addContextListenerRequest, new());
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var response = await _fdc3.HandleRemoveContextListener(request, new());

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleOpen_returns_PayloadNull_error()
    {
        OpenRequest? request = null;

        var response = await _fdc3.HandleOpen(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleOpen_returns_MissingId_error()
    {
        OpenRequest? request = new()
        {
            InstanceId = "NotExistentId"
        };

        var response = await _fdc3.HandleOpen(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task HandleOpen_returns_AppNotFound_error()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);
        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier()
            {
                AppId = "NonExistentAppId"
            }
        };

        var response = await _fdc3.HandleOpen(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenError.AppNotFound);
    }

    [Fact]
    public async Task HandleOpen_returns_AppTimeout_error_as_context_listener_is_not_registered()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);
        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier()
            {
                AppId = "appId1"
            },
            Context = JsonSerializer.Serialize(new Context("fdc3.instrument"))
        };

        var response = await _fdc3.HandleOpen(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(OpenError.AppTimeout);
    }

    [Fact]
    public async Task HandleOpen_returns_without_context()
    {
        await _fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        OpenRequest? request = new()
        {
            InstanceId = originFdc3InstanceId,
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1"
            }
        };

        var response = await _fdc3.HandleOpen(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().BeNull();
        response!.AppIdentifier.Should().NotBeNull();
        response!.AppIdentifier!.AppId.Should().Be("appId1");
        response!.AppIdentifier!.InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleGetOpenedAppContext_returns_PayloadNull_error()
    {
        GetOpenedAppContextRequest? request = null;

        var response = await _fdc3.HandleGetOpenedAppContext(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task HandleGetOpenedAppContext_returns_IdNotParsable_error()
    {
        GetOpenedAppContextRequest? request = new()
        {
            ContextId = "NotValidId"
        };

        var response = await _fdc3.HandleGetOpenedAppContext(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.IdNotParsable);
    }

    [Fact]
    public async Task HandleGetOpenedAppContext_returns_ContextNotFound_error()
    {
        GetOpenedAppContextRequest? request = new()
        {
            ContextId = Guid.NewGuid().ToString(),
        };

        var response = await _fdc3.HandleGetOpenedAppContext(request, new());

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.OpenedAppContextNotFound);
    }


    [Theory]
    [ClassData(typeof(FindIntentTheoryData))]
    public async Task HandleFindIntent_edge_case_tests(FindIntentTestCase testCase)
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
    public async Task HandleFindIntentsByContext_edge_case_tests(FindIntentsByContextTestCase testCase)
    {
        var request = testCase.Request;

        var result = await _fdc3.HandleFindIntentsByContext(request, new MessageContext());
        result.Should().NotBeNull();

        if (testCase.ExpectedAppIntentsCount > 0)
        {
            result!.AppIntents!.Should().HaveCount(testCase.ExpectedAppIntentsCount);
        }

        result!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    public class FindIntentsByContextTheoryData : TheoryData
    {
        public FindIntentsByContextTheoryData()
        {
            // Returning one AppIntent with one app by just passing Context
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("contextCustom")
                    }, //This relates to the appId4 only
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata
                                    {Name = "intentMetadataCustom", DisplayName = "intentMetadataCustom"},
                                Apps = new[]
                                {
                                    new AppMetadata {AppId = "appId4", Name = "app4", ResultType = null}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 1
                });

            // Returning one AppIntent with multiple app by just passing Context
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context2")
                    }, //This relates to the appId4, appId5, appId6,
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                                Apps = new AppMetadata[]
                                {
                                    new() {AppId = "appId4", Name = "app4", ResultType = null},
                                    new() {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                    new() {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 1
                });

            // Returning multiple appIntents by just passing Context
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context9")
                    }, //This relates to the wrongappId9 and an another wrongAppId9 with 2 individual IntentMetadata
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata9", DisplayName = "displayName9"},
                                Apps = new[]
                                {
                                    new AppMetadata
                                    {
                                        AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp"
                                    }
                                }
                            },
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata10", DisplayName = "displayName10"},
                                Apps = new[]
                                {
                                    new AppMetadata
                                        {AppId = "appId11", Name = "app11", ResultType = "channel<specified>"}
                                }
                            },
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata11", DisplayName = "displayName11"},
                                Apps = new[]
                                {
                                    new AppMetadata {AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 3
                });

            // Returning error no apps found by just passing Context
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("noAppShouldReturn")
                    }, // no app should have this context type
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    ExpectedAppIntentsCount = 0
                });

            // Returning one AppIntent with one app by ResultType
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context2"), //This relates to multiple appId
                        ResultType = "resultType<specified>"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata
                                {
                                    Name = "intentMetadata4", DisplayName = "displayName4"
                                }, // it should just return appId5
                                Apps = new[]
                                {
                                    new AppMetadata
                                        {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 1
                });

            // Returning one AppIntent with multiple apps by ResultType
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context2"), //This relates to multiple appId
                        ResultType = "resultType"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                                Apps = new[]
                                {
                                    new AppMetadata
                                        {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                    new AppMetadata {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 1
                });

            // Returning multiple AppIntents by ResultType
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context9"), //This relates to multiple appId
                        ResultType = "resultWrongApp"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata9", DisplayName = "displayName9"},
                                Apps = new[]
                                {
                                    new AppMetadata
                                        {AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp"}
                                }
                            },
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata11", DisplayName = "displayName11"},
                                Apps = new[]
                                {
                                    new AppMetadata {AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"}
                                }
                            }
                        }
                    },
                    ExpectedAppIntentsCount = 2
                });

            // Returning no apps found error by using ResultType
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context("context9"), //This relates to multiple appId
                        ResultType = "noAppShouldReturn"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    ExpectedAppIntentsCount = 0
                });

            // Returning intent delivery error
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = null,
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    },
                    ExpectedAppIntentsCount = 0
                });

            // Returning all the apps that are using the ResultType by adding fdc3.nothing.
            AddRow(
                new FindIntentsByContextTestCase
                {
                    Request = new FindIntentsByContextRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Context = new Context(ContextTypes.Nothing),
                        ResultType = "resultWrongApp"
                    },
                    ExpectedResponse = new FindIntentsByContextResponse
                    {
                        AppIntents = new[]
                        {
                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata9", DisplayName = "displayName9"},
                                Apps = new[]
                                {
                                    new AppMetadata
                                        {AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp"}
                                }
                            },

                            new AppIntent
                            {
                                Intent = new IntentMetadata {Name = "intentMetadata11", DisplayName = "displayName11"},
                                Apps = new[]
                                {
                                    new AppMetadata {AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"}
                                }
                            }
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
            AddRow(
                new FindIntentTestCase
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse
                    {
                        Error = ResolveError.NoAppsFound
                    },
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata2",
                        Context = new Context("noAppShouldBeReturned")
                    }
                });

            AddRow(
                new FindIntentTestCase
                {
                    ExpectedAppCount = 0,
                    ExpectedResponse = new FindIntentResponse
                    {
                        Error = ResolveError.IntentDeliveryFailed
                    },
                    Request = null
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata7",
                        Context = new Context("context8"),
                        ResultType = "resultType2<specified2>"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata7", DisplayName = "displayName7"},
                            Apps = new[]
                            {
                                new AppMetadata
                                    {AppId = "appId7", Name = "app7", ResultType = "resultType2<specified2>"}
                            }
                        }
                    },
                    ExpectedAppCount = 1
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata4",
                        Context = new Context("context2"),
                        ResultType = "resultType"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                            Apps = new[]
                            {
                                new AppMetadata {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                new AppMetadata {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                            }
                        }
                    },
                    ExpectedAppCount = 2
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata7",
                        ResultType = "resultType2<specified2>"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata7", DisplayName = "displayName7"},
                            Apps = new[]
                            {
                                new AppMetadata
                                    {AppId = "appId7", Name = "app7", ResultType = "resultType2<specified2>"}
                            }
                        }
                    },
                    ExpectedAppCount = 1
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata4",
                        ResultType = "resultType"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                            Apps = new[]
                            {
                                new AppMetadata {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                new AppMetadata {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                            }
                        }
                    },
                    ExpectedAppCount = 2
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata1",
                        Context = new Context("context1")
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata1", DisplayName = "displayName1"},
                            Apps = new[]
                            {
                                new AppMetadata {AppId = "appId1", Name = "app1", ResultType = null}
                            }
                        }
                    },
                    ExpectedAppCount = 1
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata4",
                        Context = new Context("context2")
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                            Apps = new AppMetadata[]
                            {
                                new() {AppId = "appId4", Name = "app4", ResultType = null},
                                new() {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                new() {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                            }
                        }
                    },
                    ExpectedAppCount = 3
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata2"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata2", DisplayName = "displayName2"},
                            Apps = new[]
                            {
                                new AppMetadata {AppId = "appId2", Name = "app2", ResultType = null}
                            }
                        }
                    },
                    ExpectedAppCount = 1
                });

            AddRow(
                new FindIntentTestCase
                {
                    Request = new FindIntentRequest
                    {
                        Fdc3InstanceId = Guid.NewGuid().ToString(),
                        Intent = "intentMetadata4"
                    },
                    ExpectedResponse = new FindIntentResponse
                    {
                        AppIntent = new AppIntent
                        {
                            Intent = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"},
                            Apps = new AppMetadata[]
                            {
                                new() {AppId = "appId4", Name = "app4", ResultType = null},
                                new() {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                                new() {AppId = "appId6", Name = "app6", ResultType = "resultType"}
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