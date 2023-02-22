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

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using Shell.Utilities;

namespace Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskCompletionSource _stopTaskSource = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private IHost? _host;

        private void StartWithWebWindowOptions(WebWindowOptions options)
        {
            var webWindow = new WebWindow(options);
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            webWindow.Show();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = new HostBuilder()
            .ConfigureAppConfiguration(
                config => config.AddJsonFile("appsettings.json"))
            .ConfigureLogging(l => l.AddDebug().SetMinimumLevel(LogLevel.Debug))
            .ConfigureServices(
                (context, services) => services
                    .AddMessageRouterServer(mr => mr.UseWebSockets())
                    .Configure<MessageRouterWebSocketServerOptions>(
                        context.Configuration.GetSection("MessageRouter:WebSocket"))
                    .Configure<LoggerFactoryOptions>(context.Configuration.GetSection("Logging")))
            .Build();

            await _host.StartAsync(_cancellationTokenSource.Token);

            if (e.Args.Length != 0
                && CommandLineParser.TryParse<WebWindowOptions>(e.Args, out var webWindowOptions)
                && webWindowOptions.Url != null)
            {
                StartWithWebWindowOptions(webWindowOptions);

                return;
            }

            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            new MainWindow().Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync(_cancellationTokenSource.Token);
                _host.Dispose();
            }
            _cancellationTokenSource.Cancel();
            _stopTaskSource.TrySetResult();
            await _stopTaskSource.Task;
            
            base.OnExit(e);
        }
    }
}
