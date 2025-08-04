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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class EndToEndTests : IAsyncLifetime
{
    private const string TestChannel = "fdc3.channel.1";
    private readonly List<IModuleInstance> _runningApps = [];
    private readonly object _runningAppsLock = new();
    private readonly ChannelTopics _topics = Fdc3Topic.UserChannel(TestChannel);
    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
    private ServiceProvider _clientServices;

    private int _counter;
    private IHost _host;
    private IMessaging _messaging;
    private IModuleLoader _moduleLoader;
    private JsonSerializerOptions _jsonOptions;
    private IDisposable _runningAppsObserver;

    private GetCurrentContextRequest RequestWithEmptyContextType => new GetCurrentContextRequest();

    private GetCurrentContextRequest RequestWithContextType => new GetCurrentContextRequest { ContextType = new Contact().Type };

    private GetCurrentContextRequest RequestWithOtherContextType => new GetCurrentContextRequest { ContextType = new Email(null).Type };

    private FindChannelRequest FindRequest => new FindChannelRequest { ChannelId = TestChannel, ChannelType = ChannelType.User };

    private FindChannelRequest FindNonExistingRequest => new FindChannelRequest { ChannelId = "nonexisting", ChannelType = ChannelType.User };
    public async Task InitializeAsync()
    {
        // Create the backend side
        IHostBuilder builder = new HostBuilder();
        builder.ConfigureServices(
            services =>
            {
                services.AddMessageRouterServer(
                    s => s.UseWebSockets(
                        opt =>
                        {
                            opt.RootPath = _webSocketUri.AbsolutePath;
                            opt.Port = _webSocketUri.Port;
                        }));
                services.AddMessageRouter(mr => mr.UseServer());

                services.AddFdc3AppDirectory(
                    _ => _.Source = new Uri(
                        AppDirectoryPath));

                services.AddModuleLoader();

                services.AddFdc3DesktopAgent(
                    fdc3 =>
                    {
                        fdc3.Configure(builder => { builder.ChannelId = TestChannel; });
                    });

                services.AddMessageRouterMessagingAdapter();
            });

        _host = builder.Build();
        await _host.StartAsync();

        // Create a client acting in place of an application
        _clientServices = new ServiceCollection()
            .AddMessageRouter(
                mr => mr.UseWebSocket(
                    new MessageRouterWebSocketOptions
                    {
                        Uri = _webSocketUri
                    }))
            .AddMessageRouterMessagingAdapter()
            .BuildServiceProvider();

        _messaging = _clientServices.GetRequiredService<IMessaging>();

        _moduleLoader = _host.Services.GetRequiredService<IModuleLoader>();

        _runningAppsObserver = _moduleLoader.LifetimeEvents.Subscribe(
            lifetimeEvent =>
            {
                lock (_runningAppsLock)
                {
                    switch (lifetimeEvent.EventType)
                    {
                        case LifetimeEventType.Started:
                            _runningApps.Add(lifetimeEvent.Instance);
                            break;

                        case LifetimeEventType.Stopped:
                            _runningApps.Remove(lifetimeEvent.Instance);
                            break;
                    }
                }
            });

        var fdc3DesktopAgentMessagingService =
            _host.Services.GetRequiredService<IHostedService>() as Fdc3DesktopAgentMessagingService;

        _jsonOptions = fdc3DesktopAgentMessagingService!.JsonMessageSerializerOptions;
    }

    public async Task DisposeAsync()
    {
        List<IModuleInstance> runningApps;
        _runningAppsObserver?.Dispose();
        lock (_runningAppsLock)
        {
            runningApps = _runningApps.Reverse<IModuleInstance>().ToList();
        }

        foreach (var instance in runningApps)
        {
            await _moduleLoader.StopModule(new StopRequest(instance.InstanceId));
        }

        await _clientServices.DisposeAsync();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact]
    public async Task GetCurrentContextReturnsNullBeforeBroadcast()
    {
        var resultBuffer = await _messaging.InvokeJsonServiceAsync<GetCurrentContextRequest, string>(_topics.GetCurrentContext, RequestWithEmptyContextType, _jsonOptions);
        resultBuffer.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentContextReturnsAfterBroadcast()
    {
        var ctx = GetContext();

        await _messaging.PublishAsync(_topics.Broadcast, ctx);

        await Task.Delay(100);

        var resultBuffer = await _messaging.InvokeServiceAsync(_topics.GetCurrentContext, JsonSerializer.Serialize(RequestWithContextType, _jsonOptions));

        resultBuffer.Should().NotBeNull();
        var result = ReadJson<Contact>(resultBuffer);

        result.Should().BeEquivalentTo(ReadJson<Contact>(ctx));
    }

    [Fact]
    public async Task GetCurrentContextReturnsAfterBroadcastWithNoType()
    {
        var ctx = GetContext();

        await _messaging.PublishAsync(_topics.Broadcast, ctx);

        await Task.Delay(100);

        var resultBuffer = await _messaging.InvokeServiceAsync(_topics.GetCurrentContext, JsonSerializer.Serialize(RequestWithEmptyContextType, _jsonOptions));

        resultBuffer.Should().NotBeNull();
        var result = ReadJson<Contact>(resultBuffer);

        result.Should().BeEquivalentTo(ReadJson<Contact>(ctx));
    }

    [Fact]
    public async Task DifferentGetCurrentContextReturnsNullAfterBroadcast()
    {
        var ctx = GetContext();
        await _messaging.PublishAsync(_topics.Broadcast, ctx);
        await Task.Delay(100);
        var resultBuffer = await _messaging.InvokeJsonServiceAsync<GetCurrentContextRequest, string>(_topics.GetCurrentContext, RequestWithOtherContextType, _jsonOptions);
        resultBuffer.Should().BeNull();
    }

    [Fact]
    public async Task FindUserChannelReturnsFoundTrueForExistingChannel()
    {
        var result = await _messaging.InvokeJsonServiceAsync<FindChannelRequest, FindChannelResponse>(Fdc3Topic.FindChannel, FindRequest, _jsonOptions);

        result.Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async Task FindUserChannelReturnsNoChannelFoundForNonExistingChannel()
    {
        var result = await _messaging.InvokeJsonServiceAsync<FindChannelRequest, FindChannelResponse>(Fdc3Topic.FindChannel, FindNonExistingRequest, _jsonOptions);

        result.Should().BeEquivalentTo(FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    [Fact]
    public async Task AddAppChannelReturnsNullWithNullChannelId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var result = await _messaging.InvokeJsonServiceAsync<CreateAppChannelRequest, CreateAppChannelResponse>(
            Fdc3Topic.CreateAppChannel,
                new CreateAppChannelRequest()
                {
                    ChannelId = null,
                    InstanceId = originFdc3InstanceId
                },
                _jsonOptions);

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(ChannelError.CreationFailed));
    }

    [Fact]
    public async Task FindIntentReturnsAppIntent()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name
        };

        var expectedResponse = new FindIntentResponse
        {
            AppIntent = new AppIntent
            {
                Intent = Intent2,
                Apps =
                [
                    App2,
                    App3ForIntent2
                ]
            }
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentRequest, FindIntentResponse>(Fdc3Topic.FindIntent, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new FindIntentResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentResponse>(Fdc3Topic.FindIntent, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "noAppShouldReturnIntent"
        };

        var expectedResponse = new FindIntentResponse
        {
            Error = ResolveError.NoAppsFound
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentRequest, FindIntentResponse>(Fdc3Topic.FindIntent, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsAppIntent()
    {
        var moduleInstance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var fdc3InstanceId = Fdc3InstanceIdRetriever.Get(moduleInstance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = SingleContext.AsJson(),
            ResultType = ResultType1
        };

        var expectedAppIntent = App1;
        expectedAppIntent.InstanceId = fdc3InstanceId;
        var expectedAppMetadata = new AppMetadata[] { expectedAppIntent, App1 };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentsByContextRequest, FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.AppIntents.Should().NotBeNull().And.HaveCount(1);
        result!.AppIntents!.First().Apps.Should().NotBeNull()
            .And.ContainEquivalentOf(expectedAppMetadata[0])
            .And.ContainEquivalentOf(expectedAppMetadata[1]);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task FindIntentsByContextReturnsMultipleAppIntents()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = MultipleContext.AsJson(),
            ResultType = ResultType2
        };

        var expectedResponse = new FindIntentsByContextResponse
        {
            AppIntents =
            [
                new AppIntent
                {
                    Intent = Intent2,
                    Apps =
                    [
                        App2
                    ]
                },
                new AppIntent
                {
                    Intent = Intent3,
                    Apps =
                    [
                        App3ForIntent3
                    ]
                }
            ]
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentsByContextRequest, FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new FindIntentsByContextResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = new Context("context2").AsJson(),
            ResultType = "noAppShouldReturn"
        };

        var expectedResponse = new FindIntentsByContextResponse
        {
            Error = ResolveError.NoAppsFound
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindIntentsByContextRequest, FindIntentsByContextResponse>(Fdc3Topic.FindIntentsByContext, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new RaiseIntentResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<RaiseIntentResponse>(Fdc3Topic.RaiseIntent, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "noIntentShouldHandle",
            Context = new Context(ContextTypes.Nothing).AsJson()
        };

        var expectedResponse = new RaiseIntentResponse
        {
            Error = ResolveError.NoAppsFound
        };

        var result = await _messaging.InvokeJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsAppIntentWithOneExistingAppAndPublishesContextToHandle()
    {
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var target = await _moduleLoader.StartModule(new StartRequest(App2.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest =
                new IntentListenerRequest
                {
                    Intent = Intent2.Name,
                    Fdc3InstanceId = targetFdc3InstanceId,
                    State = SubscribeState.Subscribe
                };

        var addIntentListenerResult = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResult.Should().BeEquivalentTo(IntentListenerResponse.SubscribeSuccess());

        var request = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var result = await _messaging.InvokeJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, request, _jsonOptions);

        result.Should().NotBeNull();

        var expectedResponse = new RaiseIntentResponse
        {
            MessageId = result!.MessageId,
            Intent = Intent2.Name,
            AppMetadata = App2
        };
        expectedResponse.AppMetadata.InstanceId = targetFdc3InstanceId;

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StoreIntentResultReturnsIntentDeliveryFailureAsRequestIsNull()
    {
        var expectedResponse = new StoreIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<StoreIntentResultResponse>(Fdc3Topic.SendIntentResult, _jsonOptions);

        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StoreIntentResultReturnsSuccessfully()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var targetInstance = await _moduleLoader.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(targetInstance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _messaging.InvokeJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, raiseIntentRequest, _jsonOptions);
        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult.AppMetadata!.InstanceId.Should().NotBeNull();

        var resultContext = new Context(ResultType2);

        var request = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = Intent2.Name,
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = resultContext.AsJson()
        };

        var expectedResponse = new StoreIntentResultResponse
        {
            Stored = true
        };

        var app2 = _runningApps.First(application => application.Manifest.Id == App2.AppId);
        var app2Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app2);
        app2Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.InstanceId);

        var result = await _messaging.InvokeJsonServiceAsync<StoreIntentResultRequest, StoreIntentResultResponse>(Fdc3Topic.SendIntentResult, request, _jsonOptions);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestIsNull()
    {
        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetIntentResultResponse>(Fdc3Topic.GetIntentResult, _jsonOptions);

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestDoesNotContainInformation()
    {
        var request = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId }
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetIntentResultRequest, GetIntentResultResponse>(Fdc3Topic.GetIntentResult, request, _jsonOptions);

        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsNoIntentResultFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "testIntent",
            TargetAppIdentifier = new AppIdentifier { AppId = App1.AppId, InstanceId = originFdc3InstanceId }
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetIntentResultRequest, GetIntentResultResponse>(Fdc3Topic.GetIntentResult, request, _jsonOptions);

        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsSuccessfully()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var targetInstance = await _moduleLoader.StartModule(new StartRequest(App2.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(targetInstance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = Intent2.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App2.AppId, InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResult = await _messaging.InvokeJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, raiseIntentRequest, _jsonOptions);

        var testContext = new Context(ResultType2);

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = Intent2.Name,
            OriginFdc3InstanceId = raiseIntentResult!.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = testContext.AsJson()
        };

        await _messaging.InvokeJsonServiceAsync<StoreIntentResultRequest, StoreIntentResultResponse>(Fdc3Topic.SendIntentResult, storeIntentRequest, _jsonOptions);

        var request = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = Intent2.Name,
            TargetAppIdentifier = new AppIdentifier
            { AppId = App2.AppId, InstanceId = raiseIntentResult!.AppMetadata!.InstanceId! }
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Context = testContext.AsJson()
        };

        var app2 = _runningApps.First(application => application.Manifest.Id == App2.AppId);
        var app2Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app2);
        app2Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.InstanceId);

        var result = await _messaging.InvokeJsonServiceAsync<GetIntentResultRequest, GetIntentResultResponse>(Fdc3Topic.GetIntentResult, request, _jsonOptions);

        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task AddIntentListenerReturnsPayloadNullError()
    {
        var result = await _messaging.InvokeJsonServiceAsync<IntentListenerResponse>(Fdc3Topic.AddIntentListener, _jsonOptions);

        result.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddIntentListenerReturnsMissingIdError()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);
        var request = new IntentListenerRequest
        { Intent = "dummy", Fdc3InstanceId = originFdc3InstanceId, State = SubscribeState.Unsubscribe };
        var response = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, request, _jsonOptions);
        response.Should().BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListenerSubscribesWithExistingAppPerRaisedIntent()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentWithNoResult",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest, _jsonOptions);

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        var raiseIntentRequest =
            new RaiseIntentRequest
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentWithNoResult",
                Context = new Context(ContextTypes.Nothing).AsJson(),
                TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            };

        var raiseIntentResult = await _messaging.InvokeJsonServiceAsync<RaiseIntentRequest, RaiseIntentResponse>(Fdc3Topic.RaiseIntent, raiseIntentRequest, _jsonOptions);

        raiseIntentResult.Should().NotBeNull();
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult.AppMetadata!.AppId.Should().Be("appId4");

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        raiseIntentResult!.AppMetadata!.InstanceId.Should().Be(app4Fdc3InstanceId);
    }

    [Fact]
    public async Task AddIntentListenerSubscribesWithNewApp()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();
    }

    [Fact]
    public async Task AddIntentListenerUnsubscribes()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResult = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeTrue();

        addIntentListenerRequest =
            new IntentListenerRequest
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Unsubscribe
            };

        addIntentListenerResult = await _messaging.InvokeJsonServiceAsync<IntentListenerRequest, IntentListenerResponse>(Fdc3Topic.AddIntentListener, addIntentListenerRequest, _jsonOptions);

        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.Stored.Should().BeFalse();
        addIntentListenerResult!.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddAppChannelReturnsSuccessfully()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId4"));
        try
        {
            var instanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new CreateAppChannelRequest
            {
                ChannelId = "my.channel",
                InstanceId = instanceId
            };

            var response = await _messaging.InvokeJsonServiceAsync<CreateAppChannelRequest, CreateAppChannelResponse>(Fdc3Topic.CreateAppChannel, request, _jsonOptions);


            response.Should().BeEquivalentTo(CreateAppChannelResponse.Created());
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task AddAppChannelFailsWithNullRequest()
    {
        CreateAppChannelRequest? request = null;
        var result = await _messaging.InvokeJsonServiceAsync<CreateAppChannelRequest?, CreateAppChannelResponse>(Fdc3Topic.CreateAppChannel, request, _jsonOptions);

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddAppChannelFailsAsTheAppOriginIsNotFound()
    {
        var request = new CreateAppChannelRequest
        {
            ChannelId = "my.channel",
            InstanceId = Guid.NewGuid().ToString()
        };

        var response = await _messaging.InvokeJsonServiceAsync<CreateAppChannelRequest, CreateAppChannelResponse>(Fdc3Topic.CreateAppChannel, request, _jsonOptions);

        response.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(ChannelError.CreationFailed));
    }

    [Fact]
    public async Task GetUserChannelsReturnsPayloadNullError()
    {
        GetUserChannelsRequest? request = null;

        var response = await _messaging.InvokeJsonServiceAsync<GetUserChannelsRequest?, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, request, _jsonOptions);

        response.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task GetUserChannelsReturnsMissingIdError()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = "NotValidId"
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetUserChannelsRequest, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, request, _jsonOptions);

        response.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task GetUserChannelsReturnsAccessDeniedAsTheInstanceIdNotFound()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = Guid.NewGuid().ToString()
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetUserChannelsRequest, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, request, _jsonOptions);

        response.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(ChannelError.AccessDenied));
    }

    [Fact]
    public async Task GetUserChannelsSucceeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetUserChannelsRequest
        {
            InstanceId = originFdc3InstanceId
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetUserChannelsRequest, GetUserChannelsResponse>(Fdc3Topic.GetUserChannels, request, _jsonOptions);

        response.Should().BeEquivalentTo(GetUserChannelsResponse.Success(
        [
            new() { Id = "fdc3.channel.1", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 1", Color = "red", Glyph = "1" } },
            new() { Id = "fdc3.channel.2", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 2", Color = "orange", Glyph = "2" } },
            new() { Id = "fdc3.channel.3", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 3", Color = "yellow", Glyph = "3" } },
            new() { Id = "fdc3.channel.4", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 4", Color = "green", Glyph = "4" }},
            new() { Id = "fdc3.channel.5", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 5", Color = "cyan", Glyph = "5" } },
            new() { Id = "fdc3.channel.6", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 6", Color = "blue", Glyph = "6" } },
            new() { Id = "fdc3.channel.7", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 7", Color = "magenta", Glyph = "7" } },
            new() { Id = "fdc3.channel.8", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata() { Name = "Channel 8", Color = "purple", Glyph = "8" } }
        ]));
    }

    [Fact]
    public async Task JoinUserChannelReturnsMissingId()
    {
        var request = new JoinUserChannelRequest
        {
            ChannelId = "test",
            InstanceId = "NotValidId"
        };

        var response = await _messaging.InvokeJsonServiceAsync<JoinUserChannelRequest, JoinUserChannelResponse>(Fdc3Topic.JoinUserChannel, request, _jsonOptions);

        response.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task JoinUserChannelReturnsNoChannelFoundError()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new JoinUserChannelRequest
            {
                ChannelId = "test",
                InstanceId = originFdc3InstanceId
            };

            var response = await _messaging.InvokeJsonServiceAsync<JoinUserChannelRequest, JoinUserChannelResponse>(Fdc3Topic.JoinUserChannel, request, _jsonOptions);

            response.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(ChannelError.NoChannelFound));
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task JoinUserChannelSucceeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new JoinUserChannelRequest
            {
                ChannelId = "fdc3.channel.1",
                InstanceId = originFdc3InstanceId
            };

            var response = await _messaging.InvokeJsonServiceAsync<JoinUserChannelRequest, JoinUserChannelResponse>(Fdc3Topic.JoinUserChannel, request, _jsonOptions);

            response.Should().BeEquivalentTo(JoinUserChannelResponse.Joined(new DisplayMetadata()
            {
                Color = "red",
                Glyph = "1",
                Name = "Channel 1"
            }));
        }
        finally
        {
            await _moduleLoader.StopModule(new(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetInfoReturnsPayLoadNullError()
    {
        GetInfoRequest? request = null;

        var result = await _messaging.InvokeJsonServiceAsync<GetInfoRequest?, GetInfoResponse>(Fdc3Topic.GetInfo, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetInfoReturnsMissingIdErrorAsRequestDidNotContain()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId
            }
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfoReturnsMissingIdErrorAsTheRequestContainedNotValidInstanceId()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = "NotValidInstanceId"
            }
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfoReturnsMissingIdErrorAsTheGivenInstanceIdIsNotFound()
    {
        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
                InstanceId = Guid.NewGuid().ToString()
            }
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfoSuccessfullyReturns()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var instanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetInfoRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                    InstanceId = instanceId
                }
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetInfoRequest, GetInfoResponse>(Fdc3Topic.GetInfo, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.ImplementationMetadata.Should().NotBeNull();
            result!.ImplementationMetadata!
                .Should()
                .BeEquivalentTo(new ImplementationMetadata
                {
                    AppMetadata = new AppMetadata
                    {
                        AppId = App1.AppId,
                        InstanceId = instanceId,
                        Description = null,
                        Icons = [],
                        Name = "app1",
                        ResultType = null,
                        Screenshots = [],
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
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task FindInstancesReturnsPayloadNullErrorAsNoRequest()
    {
        FindInstancesRequest? request = null;

        var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task FindInstancesReturnsMissingIdAsInvalidId()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
            },
            Fdc3InstanceId = "notValidInstanceId",
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task FindInstancesReturnsMissingIdErrorAsNoInstanceFound()
    {
        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
            },
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task FindInstancesReturnsNoAppsFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new FindInstancesRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = "noAppId",
                },
                Fdc3InstanceId = originFdc3InstanceId
            };

            var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.Error.Should().Be(ResolveError.NoAppsFound);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task FindInstancesSucceedsWithOneApp()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new FindInstancesRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                },
                Fdc3InstanceId = originFdc3InstanceId
            };

            var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.Instances.Should().HaveCount(1);
            result.Instances!.ElementAt(0).InstanceId.Should().Be(originFdc3InstanceId);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task FindInstancesSucceedsWithEmptyArray()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new FindInstancesRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = "appId2",
                },
                Fdc3InstanceId = originFdc3InstanceId
            };

            var result = await _messaging.InvokeJsonServiceAsync<FindInstancesRequest, FindInstancesResponse>(Fdc3Topic.FindInstances, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.Instances.Should().HaveCount(0);
            result.Error.Should().BeNull();
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetAppMetadataReturnsPayLoadNull()
    {
        GetAppMetadataRequest? request = null;

        var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest?, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetAppMetadataReturnsMissingId()
    {
        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = App1.AppId,
            },
            Fdc3InstanceId = Guid.NewGuid().ToString(),
        };

        var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetAppMetadataReturnsMissingIdErrorAsThSearchedInstanceIdIsNotValid()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetAppMetadataRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                    InstanceId = "notValidInstanceId"
                },
                Fdc3InstanceId = originFdc3InstanceId,
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetAppMetadataReturnsTargetInstanceUnavailableErrorAsTheSearchedInstanceIdNotFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetAppMetadataRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                    InstanceId = Guid.NewGuid().ToString()
                },
                Fdc3InstanceId = originFdc3InstanceId,
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

            result.Should().NotBeNull();
            result!.Error.Should().Be(ResolveError.TargetInstanceUnavailable);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetAppMetadataReturnsAppMetadataBasedOnInstanceId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetAppMetadataRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                    InstanceId = originFdc3InstanceId
                },
                Fdc3InstanceId = originFdc3InstanceId,
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

            result!.Error.Should().BeNull();
            result!.AppMetadata.Should().BeEquivalentTo(
                new AppMetadata()
                {
                    AppId = App1.AppId,
                    InstanceId = originFdc3InstanceId,
                    Name = "app1"
                });
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetAppMetadataReturnsTargetAppUnavailableErrorAsTheSearchedAppIdNotFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetAppMetadataRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = "notExistentAppId",
                },
                Fdc3InstanceId = originFdc3InstanceId,
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

            result!.Error.Should().NotBeNull();
            result!.Error.Should().Be(ResolveError.TargetAppUnavailable);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetAppMetadataReturnsAppMetadataBasedOnAppId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new GetAppMetadataRequest
            {
                AppIdentifier = new AppIdentifier
                {
                    AppId = App1.AppId,
                },
                Fdc3InstanceId = originFdc3InstanceId,
            };

            var result = await _messaging.InvokeJsonServiceAsync<GetAppMetadataRequest, GetAppMetadataResponse>(Fdc3Topic.GetAppMetadata, request, _jsonOptions);

            result!.Error.Should().BeNull();
            result!.AppMetadata.Should().BeEquivalentTo(
                new AppMetadata()
                {
                    AppId = App1.AppId,
                    Name = "app1"
                });
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task AddContextListenerReturnsPayloadNull()
    {
        AddContextListenerRequest? request = null;

        var response = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest?, AddContextListenerResponse>(Fdc3Topic.AddContextListener, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task AddContextListenerReturnsMissingId()
    {
        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = "dummyId",
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var response = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task AddContextListenerSuccessfullyRegistersContextListener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var request = new AddContextListenerRequest
            {
                Fdc3InstanceId = originFdc3InstanceId,
                ChannelId = "fdc3.channel.1",
                ChannelType = ChannelType.User
            };

            var response = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task RemoveContextListenerReturnsPayloadNullError()
    {
        RemoveContextListenerRequest? request = null;

        var response = await _messaging.InvokeJsonServiceAsync<RemoveContextListenerRequest?, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task RemoveContextListenerReturnsMissingIdError()
    {
        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = "dummyId",
            ListenerId = Guid.NewGuid().ToString(),
        };

        var response = await _messaging.InvokeJsonServiceAsync<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task RemoveContextListenerReturnsListenerNotFoundError()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var addContextListenerRequest = new AddContextListenerRequest
            {
                Fdc3InstanceId = originFdc3InstanceId,
                ChannelId = "fdc3.channel.1",
                ChannelType = ChannelType.User,
                ContextType = "fdc3.instrument"
            };

            var addContextListenerResponse = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, addContextListenerRequest, _jsonOptions);

            addContextListenerResponse.Should().NotBeNull();
            addContextListenerResponse!.Success.Should().BeTrue();

            var request = new RemoveContextListenerRequest
            {
                ContextType = null,
                Fdc3InstanceId = originFdc3InstanceId,
                ListenerId = addContextListenerResponse.Id!,
            };

            var response = await _messaging.InvokeJsonServiceAsync<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Error.Should().Be(Fdc3DesktopAgentErrors.ListenerNotFound);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task RemoveContextListenerSuccessfullyRemovesContextListener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest(App1.AppId));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            var addContextListenerRequest = new AddContextListenerRequest
            {
                Fdc3InstanceId = originFdc3InstanceId,
                ChannelId = "fdc3.channel.1",
                ChannelType = ChannelType.User,
                ContextType = null
            };

            var addContextListenerResponse = await _messaging.InvokeJsonServiceAsync<AddContextListenerRequest, AddContextListenerResponse>(Fdc3Topic.AddContextListener, addContextListenerRequest, _jsonOptions);

            addContextListenerResponse.Should().NotBeNull();
            addContextListenerResponse!.Success.Should().BeTrue();

            var request = new RemoveContextListenerRequest
            {
                ContextType = null,
                Fdc3InstanceId = originFdc3InstanceId,
                ListenerId = addContextListenerResponse.Id!,
            };

            var response = await _messaging.InvokeJsonServiceAsync<RemoveContextListenerRequest, RemoveContextListenerResponse>(Fdc3Topic.RemoveContextListener, request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task OpenReturnsPayloadNullError()
    {
        OpenRequest? request = null;

        var response = await _messaging.InvokeJsonServiceAsync<OpenRequest?, OpenResponse>(Fdc3Topic.Open, request, _jsonOptions);


        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task OpenReturnsMissingIdError()
    {
        OpenRequest? request = new()
        {
            InstanceId = "NotExistentId"
        };

        var response = await _messaging.InvokeJsonServiceAsync<OpenRequest, OpenResponse>(Fdc3Topic.Open, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task OpenReturnsAppNotFoundError()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);
            OpenRequest? request = new()
            {
                InstanceId = originFdc3InstanceId,
                AppIdentifier = new AppIdentifier()
                {
                    AppId = "NonExistentAppId"
                }
            };

            var response = await _messaging.InvokeJsonServiceAsync<OpenRequest, OpenResponse>(
                Fdc3Topic.Open,
                request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Error.Should().Be(OpenError.AppNotFound);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task OpenReturnsAppTimeoutErrorAsContextListenerIsNotRegistered()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        try
        {
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

            var response = await _messaging.InvokeJsonServiceAsync<OpenRequest, OpenResponse>(Fdc3Topic.Open, request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Error.Should().Be(OpenError.AppTimeout);
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task OpenReturnsWithoutContext()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        try
        {
            var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

            OpenRequest? request = new()
            {
                InstanceId = originFdc3InstanceId,
                AppIdentifier = new AppIdentifier
                {
                    AppId = "appId1"
                }
            };

            var response = await _messaging.InvokeJsonServiceAsync<OpenRequest, OpenResponse>(Fdc3Topic.Open, request, _jsonOptions);

            response.Should().NotBeNull();
            response!.Error.Should().BeNull();
            response!.AppIdentifier.Should().NotBeNull();
            response!.AppIdentifier!.AppId.Should().Be("appId1");
            response!.AppIdentifier!.InstanceId.Should().NotBeNull();
        }
        finally
        {
            await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
        }
    }

    [Fact]
    public async Task GetOpenedAppContextReturnsPayloadNullError()
    {
        GetOpenedAppContextRequest? request = null;

        var response = await _messaging.InvokeJsonServiceAsync<GetOpenedAppContextRequest?, GetOpenedAppContextResponse>(Fdc3Topic.GetOpenedAppContext, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.PayloadNull);
    }

    [Fact]
    public async Task GetOpenedAppContextReturnsIdNotParsableError()
    {
        GetOpenedAppContextRequest request = new()
        {
            ContextId = "NotValidId"
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetOpenedAppContextRequest, GetOpenedAppContextResponse>(Fdc3Topic.GetOpenedAppContext, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.IdNotParsable);
    }

    [Fact]
    public async Task GetOpenedAppContextReturnsContextNotFoundError()
    {
        GetOpenedAppContextRequest? request = new()
        {
            ContextId = Guid.NewGuid().ToString(),
        };

        var response = await _messaging.InvokeJsonServiceAsync<GetOpenedAppContextRequest, GetOpenedAppContextResponse>(Fdc3Topic.GetOpenedAppContext, request, _jsonOptions);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.OpenedAppContextNotFound);
    }

    private string GetContext()
    {
        return JsonSerializer.Serialize(
            new Contact(
                new ContactID { Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}" },
                name: "Testy Tester"), _jsonOptions);
    }


    private T? ReadJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
}