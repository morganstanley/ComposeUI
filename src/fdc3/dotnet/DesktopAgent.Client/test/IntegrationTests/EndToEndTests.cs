using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;
using Finos.Fdc3;
using FluentAssertions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.DisplayMetadata;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.IntegrationTests.Helpers;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.IntegrationTests;

public class EndToEndTests : IAsyncLifetime
{
    private const string TestChannel = "fdc3.channel.1";

    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
    private ServiceProvider _clientServices;
    private IHost _host;
    private IModuleLoader _moduleLoader;
    private readonly object _runningAppsLock = new();
    private readonly List<IModuleInstance> _runningApps = [];
    private IDisposable _runningAppsObserver;
    private IDesktopAgent _desktopAgent;
    public EndToEndTests()
    {
        var repoRoot = RootPathResolver.GetRepositoryRoot();
        Environment.SetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME, repoRoot, EnvironmentVariableTarget.Process);
    }

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), null);
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), null);

        // Create the backend side
        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(
            serviceCollection =>
            {
                serviceCollection.AddMessageRouterServer(
                    s => s.UseWebSockets(
                        opt =>
                        {
                            opt.RootPath = _webSocketUri.AbsolutePath;
                            opt.Port = _webSocketUri.Port;
                        }));

                serviceCollection.AddTransient<IStartupAction, MessageRouterStartupAction>();

                serviceCollection.AddMessageRouter(
                    mr => mr
                        .UseServer());

                serviceCollection.AddFdc3AppDirectory(
                    _ => _.Source = new Uri(@$"file:\\{Directory.GetCurrentDirectory()}\testAppDirectory.json"));

                serviceCollection.AddModuleLoader();

                serviceCollection.AddFdc3DesktopAgent(
                    fdc3 =>
                    {
                        fdc3.Configure(builder =>
                        {
                            builder.ChannelId = TestChannel;
                        });
                    });

                serviceCollection.AddMessageRouterMessagingAdapter();
            });

        _host = hostBuilder.Build();
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
            .AddFdc3DesktopAgentClient()
            .BuildServiceProvider();

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

        var instanceId = Guid.NewGuid().ToString();

        var instance = await _moduleLoader.StartModule(new StartRequest("appId1", new Dictionary<string, string>() { { "Fdc3InstanceId", instanceId } }));

        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "appId1");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), instanceId);

        _desktopAgent = _clientServices.GetRequiredService<IDesktopAgent>();
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

        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), null);
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), null);
    }

    [Fact]
    public async Task GetAppMetadata_throws_error_as_error_response_received()
    {
        var action = async () => await _desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "nonExistingApp" });
        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*nonExistingApp*");
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppMetadata()
    {
        var result = await _desktopAgent.GetAppMetadata(app: new AppIdentifier { AppId = "appId1" });
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(TestAppDirectoryData.DefaultApp1);
    }

    [Fact]
    public async Task GetInfo_returns_ImplementationMetadata()
    {
        var result = await _desktopAgent.GetInfo();
        result.Should().NotBeNull();
        result.AppMetadata.Should().NotBeNull();
        result.AppMetadata.AppId.Should().Be("appId1");
    }

    [Fact]
    public async Task GetInfo_throws_error_as_instance_id_not_found()
    {
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), null);
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), null);

        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "nonExistentAppId");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), Guid.NewGuid().ToString());

        var desktopAgent = _clientServices.GetRequiredService<IDesktopAgent>();

        var action = async () => await desktopAgent.GetInfo();

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{Fdc3DesktopAgentErrors.MissingId}*");
    }

    [Fact]
    public async Task JoinUserChannel_joins_to_a_user_channel()
    {
        await _desktopAgent.JoinUserChannel("fdc3.channel.1");
        var currentChannel = await _desktopAgent.GetCurrentChannel();

        currentChannel.Should().NotBeNull();
        currentChannel.Id.Should().Be("fdc3.channel.1");
    }

    [Fact]
    public async Task JoinUserChannel_joins_to_a_user_channel_and_registers_already_added_top_level_context_listeners()
    {
        var resultContexts = new List<IContext>();

        var module = await _moduleLoader.StartModule(new StartRequest("appId1-native", new Dictionary<string, string>() { { "Fdc3InstanceId", Guid.NewGuid().ToString() } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. The app broadcasts an instrument context after it joined to the fdc3.channel.1.
        //We need to wait somehow for the module to finish up the broadcast
        await Task.Delay(2000);

        var listener1 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });
        var listener2 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });

        await _desktopAgent.JoinUserChannel("fdc3.channel.1");
        var currentChannel = await _desktopAgent.GetCurrentChannel();

        currentChannel.Should().NotBeNull();
        currentChannel.Id.Should().Be("fdc3.channel.1");

        resultContexts.Should().HaveCount(2);
    }

    [Fact]
    public async Task LeaveCurrentChannel_leaves_the_joined_channel()
    {
        await _desktopAgent.JoinUserChannel("fdc3.channel.1");
        var currentChannel = await _desktopAgent.GetCurrentChannel();

        currentChannel.Should().NotBeNull();
        currentChannel.Id.Should().Be("fdc3.channel.1");

        await _desktopAgent.LeaveCurrentChannel();
        currentChannel = await _desktopAgent.GetCurrentChannel();

        currentChannel.Should().BeNull();
    }

    [Fact]
    public async Task AddContextListener_can_be_registered_multiple_times()
    {
        var listener1 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { });
        var listener2 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { });

        listener1.Should().NotBeNull();
        listener2.Should().NotBeNull();
    }

    [Fact]
    public async Task AddContextListener_can_be_registered_but_they_do_not_receive_anything_until_they_joined_to_a_channel()
    {
        var resultContexts = new List<IContext>();

        var module = await _moduleLoader.StartModule(new StartRequest("appId1-native", new Dictionary<string, string>() { { "Fdc3InstanceId", Guid.NewGuid().ToString() } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. The app broadcasts an instrument context after it joined to the fdc3.channel.1.
        var listener1 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });
        var listener2 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });

        listener1.Should().NotBeNull();
        listener2.Should().NotBeNull();
        resultContexts.Should().HaveCount(0);
    }

    [Fact]
    public async Task Broadcast_is_not_retrieved_to_the_same_instance()
    {
        var resultContexts = new List<IContext>();

        var module = await _moduleLoader.StartModule(new StartRequest("appId1-native", new Dictionary<string, string>() { { "Fdc3InstanceId", Guid.NewGuid().ToString() } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. The app broadcasts an instrument context after it joined to the fdc3.channel.1.
        //We need to wait somehow for the module to finish up the broadcast
        await Task.Delay(2000);

        await _desktopAgent.JoinUserChannel("fdc3.channel.1");

        var listener1 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });
        var listener2 = await _desktopAgent.AddContextListener<Instrument>("fdc3.instrument", (context, contextMetadata) => { resultContexts.Add(context); });
        var currentChannel = await _desktopAgent.GetCurrentChannel();

        await _desktopAgent.Broadcast(new Instrument(new InstrumentID { Ticker = $"test-instrument-{Guid.NewGuid().ToString()}" }, "test-name"));

        currentChannel.Should().NotBeNull();
        currentChannel.Id.Should().Be("fdc3.channel.1");
        resultContexts.Should().HaveCount(2); //not 4
    }

    [Fact]
    public async Task Broadcast_fails_on_not_joined_to_channel()
    {
        var action = async () => await _desktopAgent.Broadcast(new Instrument(new InstrumentID { Ticker = $"test-instrument-{Guid.NewGuid().ToString()}" }, "test-name"));

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*No current channel to broadcast the context to.*");
    }

    [Fact]
    public async Task GetUserChannels_returns_UserChannels()
    {
        var result = await _desktopAgent.GetUserChannels();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserChannels_does_not_include_channel_if_displaymetadata_is_missing()
    {
        await _host.StopAsync();

        var hostBuilder = new HostBuilder()
            .ConfigureServices(
            serviceCollection =>
            {
                serviceCollection.AddFdc3DesktopAgent(builder =>
                {
                    builder.Configure(options =>
                    {
                        options.ChannelId = "fdc3.channel.2";
                        options.UserChannelConfig = new List<ChannelItem>()
                        {
                            new ChannelItem { Id = "fdc3.channel.1", Type = ChannelType.User, DisplayMetadata = new DisplayMetadata { Color = "#FF0000", Glyph = "icon-channel-1" } },
                            new ChannelItem { Id = "fdc3.channel.2", Type = ChannelType.User }
                        }.ToArray();
                    });
                });

                serviceCollection.AddMessageRouterServer(
                            s => s.UseWebSockets(
                                opt =>
                                {
                                    opt.RootPath = _webSocketUri.AbsolutePath;
                                    opt.Port = _webSocketUri.Port;
                                }));

                serviceCollection.AddTransient<IStartupAction, MessageRouterStartupAction>();

                serviceCollection.AddMessageRouter(
                    mr => mr
                        .UseServer());

                serviceCollection.AddFdc3AppDirectory(
                    _ => _.Source = new Uri(@$"file:\\{Directory.GetCurrentDirectory()}\testAppDirectory.json"));

                serviceCollection.AddModuleLoader();
                serviceCollection.AddMessageRouterMessagingAdapter();
            });


        var host = await hostBuilder.StartAsync();
        var moduleLoader = host.Services.GetRequiredService<IModuleLoader>();

        var clientServices = new ServiceCollection()
            .AddMessageRouter(
                mr => mr.UseWebSocket(
                    new MessageRouterWebSocketOptions
                    {
                        Uri = _webSocketUri
                    }))
            .AddMessageRouterMessagingAdapter()
            .AddFdc3DesktopAgentClient();

        var serviceProvider = clientServices.BuildServiceProvider();

        var instanceId = Guid.NewGuid().ToString();

        var instance = await moduleLoader.StartModule(new StartRequest("appId1", new Dictionary<string, string>() { { "Fdc3InstanceId", instanceId } }));

        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "appId1");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), instanceId);

        var desktopAgent = serviceProvider.GetRequiredService<IDesktopAgent>();

        var result = await desktopAgent.GetUserChannels();

        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(new[]
        {
            new
            {
                Id = "fdc3.channel.1",
                Type = ChannelType.User,
                DisplayMetadata = new { Color = "#FF0000", Glyph = "icon-channel-1" }
            }
        });

        await host.StopAsync();
    }

    [Fact]
    public async Task GetOrCreateChannel_returns_channel()
    {
        var result = await _desktopAgent.GetOrCreateChannel("app-channel-1");
        result.Should().NotBeNull();
        result.Id.Should().Be("app-channel-1");
        result.Type.Should().Be(ChannelType.App);
    }

    [Fact]
    public async Task GetOrCreateChannel_throws_error_as_error_response_received()
    {
        var action = async () => await _desktopAgent.GetOrCreateChannel(string.Empty);
        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ChannelError.CreationFailed}*");
    }

    [Fact]
    public async Task GetOrCreateChannel_creates_channel_and_client_able_to_broadcast_and_receive_context()
    {
        var resultContexts = new List<IContext>();

        var appChannel = await _desktopAgent.GetOrCreateChannel("app-channel-1");

        var listener = await appChannel.AddContextListener<Instrument>("fdc3.instrument", (context, metadata) =>
        {
            resultContexts.Add(context);
        });

        var module = await _moduleLoader.StartModule(new StartRequest("appId1-native", new Dictionary<string, string>() { { "Fdc3InstanceId", Guid.NewGuid().ToString() } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only
        //We need to wait somehow for the module to finish up the broadcast
        await Task.Delay(2000);

        await appChannel.Broadcast(new Instrument(new InstrumentID { Ticker = $"test-instrument-1" }, "test-name1"));

        appChannel.Should().NotBeNull();
        appChannel.Id.Should().Be("app-channel-1");
        resultContexts.Should().HaveCount(1);
        resultContexts.Should().BeEquivalentTo(new List<IContext>() { new Instrument(new InstrumentID { Ticker = $"test-instrument-2" }, "test-name2") });
    }

    [Fact]
    public async Task FindIntent_returns_AppIntent()
    {
        var result = await _desktopAgent.FindIntent("intent1");

        result.Should().NotBeNull();
        result.Apps.Should().NotBeNullOrEmpty();
        result.Apps.Should().HaveCount(2);

        result.Apps.First().AppId.Should().Be("appId1");
        result.Apps.First().InstanceId.Should().BeNull();

        //We are starting the app1 instance for each tests, so the second app in the list should have the instanceId defined
        result.Apps.ElementAt(1).AppId.Should().Be("appId1");
        result.Apps.ElementAt(1).InstanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task FindIntent_throws_when_intent_not_found()
    {
        var act = async () => await _desktopAgent.FindIntent("notExistent");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.NoAppsFound}*");
    }

    [Fact]
    public async Task FindInstances_returns_AppIdentifier()
    {
        var result = await _desktopAgent.FindInstances(new AppIdentifier { AppId = "appId1" });

        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindInstances_throws_error_when_app_not_found()
    {
        var act = async () => await _desktopAgent.FindInstances(new AppIdentifier { AppId = "notExistent" });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.NoAppsFound}*");
    }

    [Fact]
    public async Task FindIntentsByContext_returns_AppIdentifier()
    {
        var context = new Nothing();

        var result = await _desktopAgent.FindIntentsByContext(context);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task FindIntentsByContext_throws_when_not_app_can_handle_the_context()
    {
        var context = new Currency(new CurrencyID { CURRENCY_ISOCODE = "USD" });

        var act = async () => await _desktopAgent.FindIntentsByContext(context);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.NoAppsFound}*");
    }

    [Fact]
    public async Task RaiseIntentForContext_raises_intent_and_got_resolved_as_channel()
    {
        var fdc3InstanceId = Guid.NewGuid().ToString();
        var appId = "appId1-native";
        var module = await _moduleLoader.StartModule(new StartRequest(appId, new Dictionary<string, string>() { { "Fdc3InstanceId", fdc3InstanceId } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only
        //We need to wait somehow for the module to finish up the listener registration
        await Task.Delay(2000);

        var result = await _desktopAgent.RaiseIntentForContext(new Instrument(new InstrumentID { Ticker = "AAPL" }, "Apple Inc."), new AppIdentifier { AppId = appId, InstanceId = fdc3InstanceId });
        result.Should().NotBeNull();

        var channel = await result.GetResult();
        channel.Should().NotBeNull();

        channel.Should().BeAssignableTo<IChannel>();
    }

    [Fact]
    public async Task RaiseIntentForContext_raises_intent_throws_when_no_app_could_handle_the_request()
    {
        var act = async () => await _desktopAgent.RaiseIntentForContext(new Chart(new Instrument[] { new Instrument() }));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.NoAppsFound}*");
    }

    [Fact]
    public async Task RaiseIntentForContext_raises_intent_throws_when_listener_not_registered()
    {
        var fdc3InstanceId = Guid.NewGuid().ToString();
        var appId = "appId1-native";
        var module = await _moduleLoader.StartModule(new StartRequest(appId, new Dictionary<string, string>() { { "Fdc3InstanceId", fdc3InstanceId } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only
        //We need to wait somehow for the module to finish up the listener registration
        await Task.Delay(2000);

        var act = async () => await _desktopAgent.RaiseIntentForContext(new Valuation("USD"), new AppIdentifier { AppId = appId, InstanceId = fdc3InstanceId });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.IntentDeliveryFailed}*");
    }

    [Fact]
    public async Task AddIntentListener_able_to_handle_raised_intent()
    {
        var handledContexts = new List<IContext>();

        var listener = await _desktopAgent.AddIntentListener<Valuation>("TestInstrument", (ctx, ctxM) =>
        {
            handledContexts.Add(ctx);
            return Task.FromResult<IIntentResult>(null!);
        });

        var module = await _moduleLoader.StartModule(
            new StartRequest(
                "appId1-native",
                new Dictionary<string, string>()
                {
                    { "Fdc3InstanceId", Guid.NewGuid().ToString() },
                })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only!

        await Task.Delay(3000); //We need to wait somehow for the module to finish up its actions

        handledContexts.Should().HaveCount(1);
    }

    [Fact]
    public async Task RaiseIntent_raises_intent_and_returns_IntentResolution()
    {
        var fdc3InstanceId = Guid.NewGuid().ToString();
        var appId = "appId1-native";
        var module = await _moduleLoader.StartModule(new StartRequest(appId, new Dictionary<string, string>() { { "Fdc3InstanceId", fdc3InstanceId } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only
        //We need to wait somehow for the module to finish up the listener registration
        await Task.Delay(2000);

        var resolution = await _desktopAgent.RaiseIntent("ViewInstrument", new Instrument(new InstrumentID { Ticker = "AAPL" }, "Apple Inc."), new AppIdentifier { AppId = appId, InstanceId = fdc3InstanceId });
        resolution.Should().NotBeNull();

        var result = await resolution.GetResult();
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IChannel>();
    }

    [Fact]
    public async Task RaiseIntent_throws_as_no_apps_found_to_handle()
    {
        var act = async () => await _desktopAgent.RaiseIntent("notExistentIntent", new Instrument(new InstrumentID { Ticker = "AAPL" }, "Apple Inc."));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.NoAppsFound}*");
    }

    [Fact]
    public async Task RaiseIntent_throws_when_no_listener_registered()
    {
        var fdc3InstanceId = Guid.NewGuid().ToString();
        var appId = "appId1-native";
        var module = await _moduleLoader.StartModule(new StartRequest(appId, new Dictionary<string, string>() { { "Fdc3InstanceId", fdc3InstanceId } })); // This will ensure that the DesktopAgent backend knows its an FDC3 enabled module. For test only
        //We need to wait somehow for the module to finish up the listener registration
        await Task.Delay(2000);

        var act = async () => await _desktopAgent.RaiseIntent("TestInstrument", new Valuation("USD"), new AppIdentifier { AppId = appId, InstanceId = fdc3InstanceId });

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{ResolveError.IntentDeliveryFailed}*");
    }

    [Fact]
    public async Task Open_throws_when_no_context_listener_registered_to_handle_context_when_opening()
    {
        var act = async () => await _desktopAgent.Open(new AppIdentifier { AppId = "appId1-native" }, new Chart(new Instrument[] { }));

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
        .WithMessage($"*{OpenError.AppTimeout}*");
    }

    [Fact]
    public async Task Open_returns_AppIdentifier()
    {
        var result = await _desktopAgent.Open(new AppIdentifier { AppId = "appId1-native" });
        result.Should().NotBeNull();
        result.AppId.Should().Be("appId1-native");
        result.InstanceId.Should().NotBeNull();
    }
}