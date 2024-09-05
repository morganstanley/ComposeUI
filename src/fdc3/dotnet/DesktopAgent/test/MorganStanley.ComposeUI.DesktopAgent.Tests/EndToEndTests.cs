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
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using IntentMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.IntentMetadata;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class EndToEndTests : IAsyncLifetime
{
    private const string TestChannel = "fdc3.channel.1";
    private const string AccessToken = "token";
    private readonly List<IModuleInstance> _runningApps = new();
    private readonly object _runningAppsLock = new();
    private readonly ChannelTopics _topics = Fdc3Topic.UserChannel(TestChannel);
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
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
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
            AppMetadata = new() { AppId = "appId4", InstanceId = targetFdc3InstanceId, Name = "app4", ResultType = null }
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
    public async Task StoreIntentResultReturnsSuccessfully()
    {
        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(instance);

        var targetInstance = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(targetInstance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            MessageBuffer.Factory.CreateJson(addIntentListenerRequest, _options));

        addIntentListenerBuffer.Should().NotBeNull();
        var addIntentListenerResponse = addIntentListenerBuffer!.ReadJson<IntentListenerResponse>(_options);
        addIntentListenerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
        };

        var raiseIntentResultBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.RaiseIntent,
            MessageBuffer.Factory.CreateJson(raiseIntentRequest, _options));
        raiseIntentResultBuffer.Should().NotBeNull();
        var raiseIntentResult = raiseIntentResultBuffer!.ReadJson<RaiseIntentResponse>(_options);
        raiseIntentResult!.AppMetadata.Should().NotBeNull();
        raiseIntentResult.AppMetadata!.InstanceId.Should().NotBeNull();

        var testContext = new Context("testContextType");

        var request = new StoreIntentResultRequest
        {
            MessageId = raiseIntentResult.MessageId!,
            Intent = "intentMetadataCustom",
            OriginFdc3InstanceId = raiseIntentResult.AppMetadata!.InstanceId!,
            TargetFdc3InstanceId = originFdc3InstanceId,
            Context = testContext
        };

        var expectedResponse = new StoreIntentResultResponse
        {
            Stored = true
        };

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.InstanceId);

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
            TargetAppIdentifier = new AppIdentifier { AppId = "appId1" }
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
            TargetAppIdentifier = new AppIdentifier { AppId = "appId1", InstanceId = originFdc3InstanceId }
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

        var targetInstance = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(targetInstance);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = "intentMetadataCustom",
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerBuffer = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddIntentListener,
            MessageBuffer.Factory.CreateJson(addIntentListenerRequest, _options));

        addIntentListenerBuffer.Should().NotBeNull();
        var addIntentListenerResponse = addIntentListenerBuffer!.ReadJson<IntentListenerResponse>(_options);
        addIntentListenerResponse!.Stored.Should().BeTrue();

        var raiseIntentRequest = new RaiseIntentRequest
        {
            MessageId = 2,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "intentMetadataCustom",
            Context = new Context("contextCustom"),
            TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
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
            OriginFdc3InstanceId = raiseIntentResult!.AppMetadata!.InstanceId!,
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
            { AppId = "appId4", InstanceId = raiseIntentResult!.AppMetadata!.InstanceId! }
        };

        var expectedResponse = new GetIntentResultResponse
        {
            Context = testContext
        };

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should().Be(raiseIntentResult!.AppMetadata!.InstanceId);

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
        { Intent = "dummy", Fdc3InstanceId = originFdc3InstanceId, State = SubscribeState.Unsubscribe };
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

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest
            {
                MessageId = 1,
                Fdc3InstanceId = originFdc3InstanceId,
                Intent = "intentMetadataCustom",
                Context = new Context("contextCustom"),
                TargetAppIdentifier = new AppIdentifier { AppId = "appId4", InstanceId = targetFdc3InstanceId }
            },
            _options);

        var raiseIntentResult = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, raiseIntentRequest);

        raiseIntentResult.Should().NotBeNull();
        var raiseIntentResponse = raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!;
        raiseIntentResponse.AppMetadata.Should().NotBeNull();
        raiseIntentResponse.AppMetadata!.AppId.Should().Be("appId4");

        var app4 = _runningApps.First(application => application.Manifest.Id == "appId4");
        var app4Fdc3InstanceId = Fdc3InstanceIdRetriever.Get(app4);
        app4Fdc3InstanceId.Should()
            .Be(raiseIntentResult!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadata!.InstanceId);
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

        var intentListenerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        intentListenerResponse!.Stored.Should().BeTrue();

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

        intentListenerResponse = addIntentListenerResult!.ReadJson<IntentListenerResponse>(_options);
        intentListenerResponse!.Stored.Should().BeFalse();
        intentListenerResponse!.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddAppChannelReturnsSuccessfully()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId4"));
        var instanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new CreateAppChannelRequest
        {
            ChannelId = "my.channel",
            InstanceId = instanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.CreateAppChannel,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response?.ReadJson<CreateAppChannelResponse>(_options);

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Created());

        await _moduleLoader.StopModule(new StopRequest(origin.InstanceId));
    }

    [Fact]
    public async Task AddAppChannelFailsWithNullRequest()
    {
        CreateAppChannelRequest? request = null;
        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.CreateAppChannel,
            MessageBuffer.Factory.CreateJson(request, _options));
        var result = response?.ReadJson<CreateAppChannelResponse>(_options);

        result.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(ChannelError.CreationFailed));
    }

    [Fact]
    public async Task AddAppChannelFailsAsTheAppOriginIsNotFound()
    {
        var request = new CreateAppChannelRequest
        {
            ChannelId = "my.channel",
            InstanceId = Guid.NewGuid().ToString()
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.CreateAppChannel,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response?.ReadJson<CreateAppChannelResponse>(_options);
        result.Should().BeEquivalentTo(CreateAppChannelResponse.Failed(ChannelError.CreationFailed));
    }

    [Fact]
    public async Task GetUserChannelsReturnsPayloadNullError()
    {
        GetUserChannelsRequest? request = null;

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetUserChannels,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<GetUserChannelsResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.PayloadNull));
    }

    [Fact]
    public async Task GetUserChannelsReturnsMissingIdError()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = "NotValidId"
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetUserChannels,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<GetUserChannelsResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task GetUserChannelsReturnsAccessDeniedAsTheInstanceIdNotFound()
    {
        var request = new GetUserChannelsRequest
        {
            InstanceId = Guid.NewGuid().ToString()
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetUserChannels,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<GetUserChannelsResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Failure(ChannelError.AccessDenied));
    }

    [Fact]
    public async Task GetUserChannelsSucceeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetUserChannelsRequest
        {
            InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetUserChannels,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<GetUserChannelsResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(GetUserChannelsResponse.Success(new List<ChannelItem>() 
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
    public async Task JoinUserChannelReturnsMissingId()
    {
        var request = new JoinUserChannelRequest
        {
            ChannelId = "test",
            InstanceId = "NotValidId"
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.JoinUserChannel,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<JoinUserChannelResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(Fdc3DesktopAgentErrors.MissingId));
    }

    [Fact]
    public async Task JoinUserChannelReturnsNoChannelFoundError()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new JoinUserChannelRequest
        {
            ChannelId = "test",
            InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.JoinUserChannel,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<JoinUserChannelResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Failed(ChannelError.NoChannelFound));
    }

    [Fact]
    public async Task JoinUserChannelSucceeds()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new JoinUserChannelRequest
        {
            ChannelId = "fdc3.channel.1",
            InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.JoinUserChannel,
            MessageBuffer.Factory.CreateJson(request, _options));

        var result = response!.ReadJson<JoinUserChannelResponse>(_options);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(JoinUserChannelResponse.Joined(new DisplayMetadata() {
            Color = "red",
            Glyph = "1",
            Name = "Channel 1"
        }));

        await _moduleLoader.StopModule(new(origin.InstanceId));
    }

    [Fact]
    public async Task GetInfoReturnsPayLoadNullError()
    {
        GetInfoRequest? request = null;

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetInfo,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetInfoResponse>(_options);
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
                AppId = "appId1"
            }
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetInfo,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetInfoResponse>(_options);
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
                AppId = "appId1",
                InstanceId = "NotValidInstanceId"
            }
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetInfo,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetInfoResponse>(_options);
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
                AppId = "appId1",
                InstanceId = Guid.NewGuid().ToString()
            }
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetInfo,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetInfoResponse>(_options);
        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetInfoSuccessfullyReturns()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var instanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetInfoRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
                InstanceId = instanceId
            }
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetInfo,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetInfoResponse>(_options);
        result.Should().NotBeNull();
        result!.ImplementationMetadata.Should().NotBeNull();
        result!.ImplementationMetadata!
            .Should()
            .BeEquivalentTo(new ImplementationMetadata
            {
                AppMetadata = new AppMetadata
                {
                    AppId = "appId1",
                    InstanceId = instanceId,
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
    public async Task FindInstancesReturnsPayloadNullErrorAsNoRequest()
    {
        FindInstancesRequest? request = null;

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
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
                AppId = "appId1",
            },
            Fdc3InstanceId = "notValidInstanceId",
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
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
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString()
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task FindInstancesReturnsNoAppsFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "noAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindInstancesSucceedsWithOneApp()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
        result.Should().NotBeNull();
        result!.Instances.Should().HaveCount(1);
        result.Instances!.ElementAt(0).InstanceId.Should().Be(originFdc3InstanceId);
    }

    [Fact]
    public async Task FindInstancesSucceedsWithEmptyArray()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new FindInstancesRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId2",
            },
            Fdc3InstanceId = originFdc3InstanceId
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.FindInstances,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<FindInstancesResponse>(_options);
        result.Should().NotBeNull();
        result!.Instances.Should().HaveCount(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetAppMetadataReturnsPayLoadNull()
    {
        GetAppMetadataRequest? request = null;

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

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
                AppId = "appId1",
            },
            Fdc3InstanceId = Guid.NewGuid().ToString(),
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetAppMetadataReturnsMissingIdErrorAsThSearchedInstanceIdIsNotValid()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
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

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

        result.Should().NotBeNull();
        result!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task GetAppMetadataReturnsTargetInstanceUnavailableErrorAsTheSearchedInstanceIdNotFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
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

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

        result.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.TargetInstanceUnavailable);
    }

    [Fact]
    public async Task GetAppMetadataReturnsAppMetadataBasedOnInstanceId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
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

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

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
    public async Task GetAppMetadataReturnsTargetAppUnavailableErrorAsTheSearchedAppIdNotFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "notExistentAppId",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);

        result!.Error.Should().NotBeNull();
        result!.Error.Should().Be(ResolveError.TargetAppUnavailable);
    }

    [Fact]
    public async Task GetAppMetadataReturnsAppMetadataBasedOnAppId()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new GetAppMetadataRequest
        {
            AppIdentifier = new AppIdentifier
            {
                AppId = "appId1",
            },
            Fdc3InstanceId = originFdc3InstanceId,
        };

        var response = await _messageRouter.InvokeAsync(
            Fdc3Topic.GetAppMetadata,
            MessageBuffer.Factory.CreateJson(request, _options));

        response.Should().NotBeNull();

        var result = response!.ReadJson<GetAppMetadataResponse>(_options);
        result!.Error.Should().BeNull();
        result!.AppMetadata.Should().BeEquivalentTo(
            new AppMetadata()
            {
                AppId = "appId1",
                Name = "app1"
            });
    }

    [Fact]
    public async Task AddContextListenerReturnsPayloadNull()
    {
        AddContextListenerRequest? request = null;

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<AddContextListenerResponse>(_options);

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

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<AddContextListenerResponse>(_options);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task AddContextListenerSuccessfullyRegistersContextListener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var request = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User
        };

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<AddContextListenerResponse>(_options);

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveContextListenerReturnsPayloadNullError()
    {
        RemoveContextListenerRequest? request = null;

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.RemoveContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<RemoveContextListenerResponse>(_options);

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

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.RemoveContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<RemoveContextListenerResponse>(_options);

        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.MissingId);
    }

    [Fact]
    public async Task RemoveContextListenerReturnsListenerNotFoundError()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = "fdc3.instrument"
        };

        var addContextListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddContextListener,
            MessageBuffer.Factory.CreateJson(addContextListenerRequest, _options));

        var addContextListenerResponse = addContextListenerResult!.ReadJson<AddContextListenerResponse>(_options);
        
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.RemoveContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<RemoveContextListenerResponse>(_options);
        response.Should().NotBeNull();
        response!.Error.Should().Be(Fdc3DesktopAgentErrors.ListenerNotFound);
    }

    [Fact]
    public async Task RemoveContextListenerSuccessfullyRemovesContextListener()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        var addContextListenerRequest = new AddContextListenerRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            ChannelId = "fdc3.channel.1",
            ChannelType = ChannelType.User,
            ContextType = null
        };

        var addContextListenerResult = await _messageRouter.InvokeAsync(
            Fdc3Topic.AddContextListener,
            MessageBuffer.Factory.CreateJson(addContextListenerRequest, _options));

        var addContextListenerResponse = addContextListenerResult!.ReadJson<AddContextListenerResponse>(_options);
        addContextListenerResponse.Should().NotBeNull();
        addContextListenerResponse!.Success.Should().BeTrue();

        var request = new RemoveContextListenerRequest
        {
            ContextType = null,
            Fdc3InstanceId = originFdc3InstanceId,
            ListenerId = addContextListenerResponse.Id!,
        };

        var result = await _messageRouter.InvokeAsync(
            Fdc3Topic.RemoveContextListener,
            MessageBuffer.Factory.CreateJson(request, _options));

        var response = result!.ReadJson<RemoveContextListenerResponse>(_options);

        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    private MessageBuffer GetContext()
    {
        return MessageBuffer.Factory.CreateJson(
            new Contact(
                new ContactID {Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}"},
                name: "Testy Tester"), _options);
    }
}