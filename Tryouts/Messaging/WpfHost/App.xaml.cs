// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;

namespace WpfHost;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();

        hostBuilder
            .ConfigureServices(
                (context, services) =>
                {
                    services
                        .AddSingleton<MainWindow>()
                        .AddMessageRouterServer(mr => mr.UseWebSockets(ws => ws.RootPath = "/ws"));
                })
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Debug).AddProvider(new MainWindowLoggerProvider(this)));


        Host = hostBuilder.Build();
        Logger = Host.Services.GetRequiredService<ILogger<App>>();
    }

    public new MainWindow? MainWindow
    {
        get => (MainWindow?)base.MainWindow;
        set => base.MainWindow = value;
    }

    internal IHost Host { get; private set; }
    internal ILogger<App> Logger { get; }

    internal async void OnMainWindowClosing(object sender, CancelEventArgs args)
    {
        if (_shutdownCompleted)
            return;

        if (_isClosing)
        {
            args.Cancel = true;

            return;
        }

        _isClosing = true;
        args.Cancel = true;
        Logger.LogInformation("Main window closed, shutting down");
        await OnShutdown();
        _shutdownCompleted = true;
        Logger.LogDebug("Application shutdown complete, closing main window");
        this.MainWindow?.Close();
    }

    private bool _isClosing;
    private bool _shutdownCompleted;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await Host.StartAsync();

        MainWindow = Host.Services.GetRequiredService<MainWindow>();
        MainWindow.Show();

        var server = Host.Services.GetRequiredService<IMessageRouterWebSocketServer>();
        Logger.LogInformation("Server listening at {url}", server.WebSocketUrl);

        var messageRouter = new ServiceCollection()
            .AddMessageRouter(
                mr => mr.UseWebSocket(
                    new MessageRouterWebSocketOptions
                    {
                        Uri = server.WebSocketUrl
                    }))
            .BuildServiceProvider()
            .GetRequiredService<IMessageRouter>();

        await messageRouter.ConnectAsync().ConfigureAwait(false);
        Logger.LogInformation("Message Router client connected");
        await messageRouter.PublishAsync("ApplicationStarted").ConfigureAwait(false);
        Logger.LogInformation("Message Router publish successful");
    }

    private async Task OnShutdown()
    {
        await Host.StopAsync();
    }
}
