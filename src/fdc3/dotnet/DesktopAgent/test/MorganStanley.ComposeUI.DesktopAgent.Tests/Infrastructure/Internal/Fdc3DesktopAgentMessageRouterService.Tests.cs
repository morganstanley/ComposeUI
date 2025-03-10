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
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;
using System.Collections.Concurrent;
using System.Text.Json;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public partial class Fdc3DesktopAgentMessageRouterServiceTests : IAsyncLifetime
{
    private const string TestChannel = "fdc3.channel.1";

    private readonly IAppDirectory _appDirectory = new AppDirectory.AppDirectory(
        new AppDirectoryOptions
        {
            Source = new Uri(AppDirectoryPath)
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

    private FindChannelRequest FindTestChannel => new() { ChannelId = "fdc3.channel.1", ChannelType = ChannelType.User };

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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId }
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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();

        _mockModuleLoader.Verify(_ => _.StartModule(It.IsAny<StartRequest>()));
        result.Should().NotBeNull();
        result!.Error.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.IntentDeliveryFailed); //Due no intent handler was registered by the started app.
    }

    [Fact]
    public async Task HandleRaiseIntent_returns_one_app_by_AppIdentifier_and_saves_context_to_resolve_it_when_registers_its_intentHandler()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
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
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();
        result!.AppMetadata.Should().NotBeNull();
        result!.AppMetadata!.AppId.Should().Be(App1.AppId);
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
                Fdc3Topic.RaiseIntentResolution(Intent1.Name, targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<InvokeOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleRaiseIntent_returns_one_app_by_AppIdentifier_and_publishes_context_to_resolve_it_when_registers_its_intentHandler()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
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
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
        };

        var result = await _fdc3.HandleRaiseIntent(request, new MessageContext());
        result.Should().NotBeNull();
        result!.AppMetadata.Should().NotBeNull();
        result!.AppMetadata!.AppId.Should().Be(App1.AppId);
        result!.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution(Intent1.Name, targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleRaiseIntent_calls_ResolverUI_by_Context_filter()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson()
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        _mockResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleRaiseIntent_calls_ResolverUI_by_Context_filter_if_fdc3_nothing()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson()
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        _mockResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "testIntent",
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = "noAppShouldReturn" }
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.TargetAppUnavailable);
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_Context()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = Intent1.Name,
            Context = new Context("noAppShouldReturn").AsJson()
        };

        var result = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task HandleRaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "noAppShouldReturn",
            Context = SingleContext.AsJson()
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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App6.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithChannelResult.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithChannelResult.Name,
            Context = ChannelContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App6.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.Error.Should().BeNull();
        raiseIntentResult.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = IntentWithChannelResult.Name,
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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.Error.Should().BeNull();
        raiseIntentResult.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = Intent1.Name,
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = Guid.NewGuid().ToString(),
            ChannelId = null,
            ChannelType = null,
            Context = SingleContext.AsJson()
        };

        var result = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new StoreIntentResultResponse { Stored = true });
    }

    [Fact]
    public async Task HandleStoreIntentResult_succeeds_with_voidResult()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            TargetAppIdentifier = new AppIdentifier { AppId = App4.AppId, InstanceId = targetFdc3InstanceId },
            Context = ContextType.Nothing.AsJson()
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = IntentWithNoResult.Name,
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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var resultContext = new Context(ResultType2);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = Intent2.Name,
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = resultContext.AsJson()
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier
            { AppId = App2.AppId, InstanceId = raiseIntentResult.AppMetadata!.InstanceId! },
            Version = "1.0"
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new GetIntentResultResponse { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_context()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var resultContext = new Context(ResultType2);
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = Intent2.Name,
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = resultContext.AsJson()
        };

        var storeResult = await _fdc3.HandleStoreIntentResult(storeIntentRequest, new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = Intent2.Name,
            TargetAppIdentifier = new AppIdentifier
            { AppId = App1.AppId, InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(GetIntentResultResponse.Success(context: resultContext.AsJson()));
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_channel()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var channelType = ChannelType.User;
        var channelId = "dummyChannelId";
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent1.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent1.Name,
            Context = SingleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = Intent1.Name,
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
            Intent = Intent1.Name,
            TargetAppIdentifier = new AppIdentifier
            { AppId = App1.AppId, InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
        };

        var result = await _fdc3.HandleGetIntentResult(getIntentResultRequest, new MessageContext());
        result.Should().NotBeNull();
        result!.Should()
            .BeEquivalentTo(GetIntentResultResponse.Success(channelType: channelType, channelId: channelId));
    }

    [Fact]
    public async Task HandleGetIntentResult_succeeds_with_voidResult()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App5.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
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
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App5.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = IntentWithNoResult.Name,
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
            Intent = IntentWithNoResult.Name,
            TargetAppIdentifier = new AppIdentifier
            { AppId = App5.AppId, InstanceId = raiseIntentResult.AppMetadata!.InstanceId! }
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
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
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
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult.AppMetadata!.AppId.Should().Be(App2.AppId);
        raiseIntentResult.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution(Intent2.Name, targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAddIntentListener_subscribes()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _mockModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _mockModuleLoader.Object.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
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
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _fdc3.HandleRaiseIntent(raiseIntentRequest, new MessageContext());
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult!.AppMetadata!.AppId.Should().Be(App2.AppId);
        raiseIntentResult!.AppMetadata.InstanceId.Should().Be(targetFdc3InstanceId);

        _mockMessageRouter.Verify(
            _ => _.PublishAsync(
                Fdc3Topic.RaiseIntentResolution(Intent2.Name, targetFdc3InstanceId),
                It.IsAny<MessageBuffer>(),
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAddIntentListener_unsubscribes()
    {
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

        result!.Error.Should().BeNull();
        result!.AppMetadata.Should().BeEquivalentTo(
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

        result.Should().BeEquivalentTo(testCase.ExpectedResponse, because: testCase.Name);
    }

    [Theory]
    [ClassData(typeof(FindIntentsByContextTheoryData))]
    public async Task HandleFindIntentsByContext_edge_case_tests(FindIntentsByContextTestCase testCase)
    {
        var request = testCase.Request;

        var result = await _fdc3.HandleFindIntentsByContext(request, new MessageContext());

        result.Should().BeEquivalentTo(testCase.ExpectedResponse, because: testCase.Name);
    }
}