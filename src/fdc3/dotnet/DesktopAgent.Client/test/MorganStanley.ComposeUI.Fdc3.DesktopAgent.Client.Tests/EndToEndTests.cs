using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;
using Finos.Fdc3;
using FluentAssertions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using Castle.DynamicProxy.Generators;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests;

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

    public async Task InitializeAsync()
    {
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
                serviceCollection.AddMessageRouter(mr => mr.UseServer());

                serviceCollection.AddFdc3AppDirectory(
                    _ => _.Source = new Uri(@$"file:\\{Directory.GetCurrentDirectory()}\testAppDirectory.json"));

                serviceCollection.AddModuleLoader();

                serviceCollection.AddFdc3DesktopAgent(
                    fdc3 =>
                    {
                        fdc3.Configure(builder => { builder.ChannelId = TestChannel; });
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

        var instance = await _moduleLoader.StartModule(new StartRequest("appId1"));
        var fdc3StartupProperties = instance.GetProperties<Fdc3StartupProperties>().FirstOrDefault();

        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "appId1");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), fdc3StartupProperties.InstanceId);

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
    }

    [Fact]
    public async Task GetAppMetadata_throws_error_as_error_response_received()
    {
        var action = async () => await _desktopAgent.GetAppMetadata(new Shared.Protocol.AppIdentifier { AppId = "nonExistingApp" });
        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*nonExistingApp*");
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppMetadata()
    {
        var result = await _desktopAgent.GetAppMetadata(new Shared.Protocol.AppIdentifier { AppId = "appId1" });
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
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "nonExistentAppId");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), Guid.NewGuid().ToString());

        var desktopAgent = new DesktopAgentClient(_clientServices.GetRequiredService<IMessaging>());

        var action = async() => await desktopAgent.GetInfo();

        await action.Should()
            .ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage($"*{Fdc3DesktopAgentErrors.MissingId}*");
    }
}