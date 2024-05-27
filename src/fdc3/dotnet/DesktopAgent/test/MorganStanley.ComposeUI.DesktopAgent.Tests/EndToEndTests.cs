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
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using IntentMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.IntentMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class EndToEndTests : IAsyncLifetime
{
    private const string TestChannel = "testChannel";
    private const string AccessToken = "token";
    private readonly List<IModuleInstance> _runningApps = new();
    private readonly object _runningAppsLock = new();
    private readonly UserChannelTopics _topics = new(TestChannel);
    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
    private ServiceProvider _clientServices;

    private int _counter;
    private IHost _host;
    private IMessageRouter _messageRouter;
    private IModuleLoader _moduleLoader;
    private JsonSerializerOptions _options;
    private IDisposable _runningAppsObserver;

    private MessageBuffer EmptyContextType => MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest());

    private MessageBuffer ContextType =>
        MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest {ContextType = new Contact().Type});

    private MessageBuffer OtherContextType =>
        MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest {ContextType = new Email(null).Type});

    private MessageBuffer FindRequest => MessageBuffer.Factory.CreateJson(
        new FindChannelRequest {ChannelId = TestChannel, ChannelType = ChannelType.User});

    private MessageBuffer FindNonExistingRequest => MessageBuffer.Factory.CreateJson(
        new FindChannelRequest {ChannelId = "nonexisting", ChannelType = ChannelType.User});

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
                        $"file:\\\\{Directory.GetCurrentDirectory()}\\TestUtils\\appDirectorySample.json"));

                services.AddModuleLoader();

                services.AddFdc3DesktopAgent(
                    fdc3 =>
                    {
                        fdc3.Configure(builder => { builder.ChannelId = TestChannel; });
                        fdc3.UseMessageRouter();
                    });
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
            .BuildServiceProvider();

        _messageRouter = _clientServices.GetRequiredService<IMessageRouter>();

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

        var fdc3DesktopAgentMessageRouterService =
            _host.Services.GetRequiredService<IHostedService>() as Fdc3DesktopAgentMessageRouterService;
        _options = fdc3DesktopAgentMessageRouterService!.JsonMessageSerializerOptions;
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
    public async void GetCurrentContextReturnsNullBeforeBroadcast()
    {
        var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, EmptyContextType);
        resultBuffer.Should().BeNull();
    }

    [Fact]
    public async void GetCurrentContextReturnsAfterBroadcast()
    {
        var ctx = GetContext();

        await _messageRouter.PublishAsync(_topics.Broadcast, ctx);

        await Task.Delay(100);

        var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, ContextType);

        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<Contact>();

        result.Should().BeEquivalentTo(ctx.ReadJson<Contact>());
    }

    [Fact]
    public async void GetCurrentContextReturnsAfterBroadcastWithNoType()
    {
        var ctx = GetContext();

        await _messageRouter.PublishAsync(_topics.Broadcast, ctx);

        await Task.Delay(100);

        var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, EmptyContextType);

        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<Contact>();

        result.Should().BeEquivalentTo(ctx.ReadJson<Contact>());
    }

    [Fact]
    public async void DifferentGetCurrentContextReturnsNullAfterBroadcast()
    {
        var ctx = GetContext();
        await _messageRouter.PublishAsync(_topics.Broadcast, ctx);
        await Task.Delay(100);
        var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, OtherContextType);
        resultBuffer.Should().BeNull();
    }

    [Fact]
    public async void FindUserChannelReturnsFoundTrueForExistingChannel()
    {
        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindChannel, FindRequest);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindChannelResponse>(_options);
        result.Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async void FindUserChannelReturnsNoChannelFoundForNonExistingChannel()
    {
        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindChannel, FindNonExistingRequest);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindChannelResponse>(_options);
        result.Should().BeEquivalentTo(FindChannelResponse.Failure(ChannelError.NoChannelFound));
    }

    [Fact]
    public async Task FindIntentReturnsAppIntent()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var appId4IntentMetadata = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"};

        var request = new FindIntentRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadata4"
        };

        var expectedResponse = new FindIntentResponse
        {
            AppIntent = new AppIntent
            {
                Intent = appId4IntentMetadata,
                Apps = new AppMetadata[]
                {
                    new() {AppId = "appId4", Name = "app4", ResultType = null},
                    new() {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                    new() {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                }
            }
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindIntent,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new FindIntentResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntent);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
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

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindIntent,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsAppIntent()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = new Context("context2"),
            ResultType = "resultType"
        };

        var appId5IntentMetadata = new IntentMetadata {Name = "intentMetadata4", DisplayName = "displayName4"};

        var expectedResponse = new FindIntentsByContextResponse
        {
            AppIntents = new AppIntent[]
            {
                new()
                {
                    Intent = appId5IntentMetadata,
                    Apps = new AppMetadata[]
                    {
                        new() {AppId = "appId5", Name = "app5", ResultType = "resultType<specified>"},
                        new() {AppId = "appId6", Name = "app6", ResultType = "resultType"}
                    }
                }
            }
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindIntentsByContext,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsMultipleAppIntents()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = new Context("context9"),
            ResultType = "resultWrongApp"
        };

        var appId9IntentMetadata = new IntentMetadata {Name = "intentMetadata9", DisplayName = "displayName9"};
        var appId12IntentMetadata = new IntentMetadata {Name = "intentMetadata11", DisplayName = "displayName11"};
        var expectedResponse = new FindIntentsByContextResponse
        {
            AppIntents = new[]
            {
                new AppIntent
                {
                    Intent = appId9IntentMetadata,
                    Apps = new[]
                    {
                        new AppMetadata {AppId = "wrongappId9", Name = "app9", ResultType = "resultWrongApp"}
                    }
                },
                new AppIntent
                {
                    Intent = appId12IntentMetadata,
                    Apps = new[]
                    {
                        new AppMetadata {AppId = "appId12", Name = "app12", ResultType = "resultWrongApp"}
                    }
                }
            }
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindIntentsByContext,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new FindIntentsByContextResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntentsByContext);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task FindIntentsByContextReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new FindIntentsByContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = new Context("context2"),
            ResultType = "noAppShouldReturn"
        };

        var expectedResponse = new FindIntentsByContextResponse
        {
            Error = ResolveError.NoAppsFound
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindIntentsByContext,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
    {
        var expectedResponse = new RaiseIntentResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsNoAppsFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "noIntentShouldHandle",
            Selected = false,
            Context = new Context(ContextTypes.Nothing)
        };

        var expectedResponse = new RaiseIntentResponse
        {
            Error = ResolveError.NoAppsFound
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsAppIntentWithOneApp()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom")
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(request, _options));
        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should()
            .Be(resultBuffer!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First().InstanceId);
        app4Fdc3InstanceId.Should().NotBeNull();

        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        result.Should().NotBeNull();

        var expectedResponse = new RaiseIntentResponse
        {
            MessageId = result!.MessageId,
            Intent = "intentMetadataCustom",
            AppMetadata = new AppMetadata[]
            {
                new() {AppId = "appId4", InstanceId = app4Fdc3InstanceId, Name = "app4", ResultType = null}
            }
        };

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RaiseIntentReturnsAppIntentWithOneExistingAppAndPublishesContextToHandle()
    {
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest =
            MessageBuffer.Factory.CreateJson(
                new IntentListenerRequest
                {
                    Intent = "intentMetadataCustom",
                    Fdc3InstanceId = targetFdc3InstanceId,
                    State = SubscribeState.Subscribe
                },
                _options);

        var addIntentListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest);
        addIntentListenerResult.Should().NotBeNull();
        addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options)
            .Should()
            .BeEquivalentTo(IntentListenerResponse.SubscribeSuccess());

        var request = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier {AppId = "appId4", InstanceId = targetFdc3InstanceId}
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(request, _options));

        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        result.Should().NotBeNull();

        var expectedResponse = new RaiseIntentResponse
        {
            MessageId = result!.MessageId,
            Intent = "intentMetadataCustom",
            AppMetadata = new AppMetadata[]
            {
                new() {AppId = "appId4", InstanceId = targetFdc3InstanceId, Name = "app4", ResultType = null}
            }
        };

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StoreIntentResultReturnsIntentDeliveryFailureAsRequestIsNull()
    {
        var expectedResponse = new StoreIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.SendIntentResult);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StoreIntentResultReturnsIntentDeliveryFailureAsRequestNotContainsInformation()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new StoreIntentResultRequest
        {
            MessageId = string.Empty,
            Intent = "dummyIntent",
            OriginFdc3InstanceId = originFdc3InstanceId,
            TargetFdc3InstanceId = null
        };

        var expectedResponse = new StoreIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.SendIntentResult,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task StoreIntentResultReturnsSuccessfully()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom")
        };

        var raiseIntentResultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(raiseIntentRequest, _options));
        raiseIntentResultBuffer.Should().NotBeNull();
        var raiseIntentResult = raiseIntentResultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResult!.AppMetadata.Should().HaveCount(1);
        raiseIntentResult.AppMetadata!.First().InstanceId.Should().NotBeNull();

        var testContext = new Context("testContextType");

        var request = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadataCustom",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = testContext
        };

        var expectedResponse = new StoreIntentResultResponse
        {
            Stored = true
        };

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.First().InstanceId);

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.SendIntentResult,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestIsNull()
    {
        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.GetIntentResult);
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestDoesNotContainInformation()
    {
        var request = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "dummy",
            TargetAppIdentifier = new AppIdentifier {AppId = "appId1"}
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetIntentResult,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsIntentDeliveryFailureAsNoIntentResultFound()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var request = new GetIntentResultRequest
        {
            MessageId = "dummy",
            Intent = "testIntent",
            TargetAppIdentifier = new AppIdentifier {AppId = "appId1", InstanceId = originFdc3InstanceId}
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Error = ResolveError.IntentDeliveryFailed
        };

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetIntentResult,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetIntentResultReturnsSuccessfully()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Selected = false,
            Context = new Context("contextCustom")
        };

        var resultRaiseIntentBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(raiseIntentRequest, _options));
        var raiseIntentResult = resultRaiseIntentBuffer!.ReadJson<RaiseIntentResponse>(_options);

        var testContext = new Context("testContextType");

        var storeIntentRequest = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = "intentMetadataCustom",
            OriginFdc3InstanceId = raiseIntentResult!.AppMetadata!.First().InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = testContext
        };

        await _messageRouter.InvokeAsync(
            Fdc3Topic.SendIntentResult,
            MessageBuffer.Factory.CreateJson(storeIntentRequest, _options));

        var request = new GetIntentResultRequest
        {
            MessageId = raiseIntentResult!.MessageId!,
            Intent = "intentMetadataCustom",
            TargetAppIdentifier = new AppIdentifier
                {AppId = "appId4", InstanceId = raiseIntentResult!.AppMetadata!.First().InstanceId!}
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Context = testContext
        };

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.First().InstanceId);

        var resultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetIntentResult,
            MessageBuffer.Factory.CreateJson(request, _options));
        resultBuffer.Should().NotBeNull();
        var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
        result!.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task AddIntentListenerReturnsPayloadNullError()
    {
        var result = await _messageRouter.InvokeAsync(Fdc3Topic.AddIntentListener);
        result.Should().NotBeNull();
        result!.ReadJson<IntentListenerResponse>(_options)
            .Should()
            .BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task AddIntentListenerReturnsMissingIdError()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);
        var request = new IntentListenerRequest
            {Intent = "dummy", Fdc3InstanceId = originFdc3InstanceId, State = SubscribeState.Unsubscribe};
        var expectedResponse = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            MessageBuffer.Factory.CreateJson(request, _options));
        expectedResponse.Should().NotBeNull();
        expectedResponse!.ReadJson<IntentListenerResponse>(_options)
            .Should()
            .BeEquivalentTo(IntentListenerResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task AddIntentListenerSubscribesWithExistingAppPerRaisedIntent()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Selected = false,
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier {AppId = "appId4", InstanceId = targetFdc3InstanceId}
            },
            _options);

        var raiseIntentResult = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, raiseIntentRequest);

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!;
        raiseIntentResponse.AppMetadata.Should().HaveCount(1);
        raiseIntentResponse.AppMetadata!.First()!.AppId.Should().Be("appId4");

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            },
            _options);

        var addIntentListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest);
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListenerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListenerResponse!.Stored.Should().BeTrue();

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should()
            .Be(raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.First().InstanceId);
    }

    [Fact]
    public async Task AddIntentListenerSubscribesWithNewApp()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            },
            _options);

        var addIntentListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest);
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListenerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListenerResponse!.Stored.Should().BeTrue();
    }

    [Fact]
    public async Task AddIntentListenerUnsubscribes()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Subscribe
            },
            _options);

        var addIntentListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest);
        addIntentListenerResult.Should().NotBeNull();

        var addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeTrue();

        addIntentListenerRequest = MessageBuffer.Factory.CreateJson(
            new IntentListenerRequest
            {
                Intent = "intentMetadataCustom",
                Fdc3InstanceId = targetFdc3InstanceId,
                State = SubscribeState.Unsubscribe
            },
            _options);

        addIntentListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            addIntentListenerRequest);
        addIntentListenerResult.Should().NotBeNull();

        addIntentListnerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        addIntentListnerResponse!.Stored.Should().BeFalse();
        addIntentListnerResponse!.Error.Should().BeNull();
    }

    private MessageBuffer GetContext()
    {
        return MessageBuffer.Factory.CreateJson(
            new Contact(
                new ContactID {Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}"},
                name: "Testy Tester"));
    }
}