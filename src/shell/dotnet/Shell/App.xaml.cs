// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using Shell.Utilities;

namespace Shell;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public new static App Current => (App) Application.Current;

    public IHost Host =>
        _host
        ?? throw new InvalidOperationException(
            "Attempted to access the Host object before async startup has completed");
    
    /// <summary>
    /// Creates a new window of the specified type. Constructor arguments that are not registered in DI can be provided.
    /// </summary>
    /// <typeparam name="TWindow">The type of the window</typeparam>
    /// <param name="parameters">Any constructor arguments that are not registered in DI</param>
    /// <returns>A new instance of the window.</returns>
    /// <exception cref="InvalidOperationException">The method was not called from the UI thread.</exception>
    public TWindow CreateWindow<TWindow>(params object[] parameters) where TWindow : Window
    {
        Dispatcher.VerifyAccess();

        return ActivatorUtilities.CreateInstance<TWindow>(Host.Services, parameters);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Task.Run(() => StartAsync(e));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        var stopCompletedEvent = new ManualResetEventSlim();
        _ = Task.Run(() => StopAsync(stopCompletedEvent));
        _logger.LogDebug("Waiting for async shutdown");
        stopCompletedEvent.Wait();
        _logger.LogDebug("Async shutdown completed, application will now exit");
    }

    private IHost? _host;
    private ILogger _logger = NullLogger<App>.Instance;
    private string _messageRouterAccessToken = Guid.NewGuid().ToString("N");

    private async Task StartAsync(StartupEventArgs e)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(
                config => config.AddJsonFile("appsettings.json", optional: true))
            .ConfigureLogging(l => l.AddDebug().SetMinimumLevel(LogLevel.Debug))
            .ConfigureServices(ConfigureServices)
            .Build();

        await host.StartAsync();
        _host = host;
        _logger = _host.Services.GetRequiredService<ILogger<App>>();

        await OnHostInitializedAsync();

        Dispatcher.Invoke(() => OnAsyncStartupCompleted(e));
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // TODO: Extensibility: plugins should be able to configure the service collection.
        services.AddMessageRouterServer(
            mr => mr
                .UseWebSockets()
                .UseAccessTokenValidator(
                    (clientId, token) =>
                    {
                        // TODO: Assign a separate token for each client and only allow a single connection with each token
                        if (_messageRouterAccessToken != token)
                            throw new InvalidOperationException("The provided access token is invalid");
                    }));

        services.AddMessageRouter(
            mr => mr
                .UseServer()
                .UseAccessToken(_messageRouterAccessToken));


        services.AddFdc3DesktopAgent(fdc3 => fdc3.WithUserChannel("default"));

        services.Configure<LoggerFactoryOptions>(context.Configuration.GetSection("Logging"));
    }

    // TODO: Extensibility: Plugins should be notified here.
    // Add any feature-specific async init code that depends on a running Host to this method 
    private async Task OnHostInitializedAsync()
    {
        InjectMessageRouterConfig();
        InjectFDC3();
    }

    private void OnAsyncStartupCompleted(StartupEventArgs e)
    {
        if (e.Args.Length != 0
            && CommandLineParser.TryParse<WebWindowOptions>(e.Args, out var webWindowOptions)
            && webWindowOptions.Url != null)
        {
            StartWithWebWindowOptions(webWindowOptions);

            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        CreateWindow<MainWindow>().Show();
    }

    private async Task StopAsync(ManualResetEventSlim stopCompletedEvent)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        stopCompletedEvent.Set();
    }

    private void StartWithWebWindowOptions(WebWindowOptions options)
    {
        ShutdownMode = ShutdownMode.OnLastWindowClose;
        CreateWindow<WebWindow>(options).Show();
    }

    private void InjectMessageRouterConfig()
    {
        var server = Host.Services.GetRequiredService<IMessageRouterWebSocketServer>();
        _logger.LogInformation($"Message Router server listening at {server.WebSocketUrl}");

        WebWindow.AddPreloadScript(
            $$"""
                window.composeui = {
                    ...window.composeui,
                    messageRouterConfig: {
                        accessToken: "{{JsonEncodedText.Encode(_messageRouterAccessToken)}}",
                        webSocket: {
                            url: "{{server.WebSocketUrl}}"
                        }
                    }
                };
                    
                """);
    }
   
    private void InjectFDC3()
    {
        string bundle = @"fdc3-iife-bundle.js";
        string iife = ReadResource(bundle);

        WebWindow.AddPreloadScript(iife);
    }

    public string ReadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();    
        string resourcePath = assembly.GetManifestResourceNames()
            .Single(str => str.EndsWith(name));
        
        using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}
