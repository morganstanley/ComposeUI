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

using WPFDataGrid.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ProcessExplorer.LocalCollector.DependencyInjection;
using System.Collections.Generic;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Connections;

namespace WPFDataGrid.TestApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Url to connect
    /// </summary>
    public static Uri WebsocketURI { get; set; } = new("ws://localhost:5000/ws");
    public static Guid WebsocketURIId { get; } = Guid.NewGuid();

    /// <summary>
    /// Overriding Statup so we can do DI.
    /// </summary>
    /// <param name="e"></param>
    protected async override void OnStartup(StartupEventArgs e)
    {
        ServiceCollection serviceCollection = new();

        base.OnStartup(e);

        if (e.Args.Any() && Uri.TryCreate(e.Args[0], UriKind.Absolute, out Uri? uri))
        {
            WebsocketURI = uri;
        }

        var loggerFactory = new LoggerFactory();

        var serilogger = new LoggerConfiguration()
            .WriteTo.File($"{Directory.GetCurrentDirectory()}/log.log")
            .CreateLogger();

        serviceCollection
            .AddLogging(
                builder =>
                {
                    builder.AddSerilog(serilogger);
                })
            .AddMessageRouter(
                mr =>
                    mr.UseWebSocket(new MessageRouterWebSocketOptions { Uri = WebsocketURI }));

        serviceCollection
            .AddLocalCollectorWithGrpc(localCollector => 
            localCollector.UseGrpc(new LocalCollectorServiceOptions
                {
                    Connections = new List<IConnectionInfo>()
                    {
                        new ConnectionInfo(
                            id: WebsocketURIId,
                            name: nameof(WebsocketURI),
                            status: ConnectionStatus.Running,
                            remoteEndpoint: WebsocketURI.ToString())
                    },
                    LoadedServices = serviceCollection,
                    Port = 5056,
                    Host = "localhost"
                }));

serviceCollection.AddSingleton(typeof(DataGridView));

_serviceProvider = serviceCollection.BuildServiceProvider();

var dataGridView = _serviceProvider?.GetRequiredService<DataGridView>();

dataGridView?.Show();
    }
}
