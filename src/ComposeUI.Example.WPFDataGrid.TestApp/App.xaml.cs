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

using ComposeUI.Example.WPFDataGrid.Views;
using ComposeUI.Messaging.Client;
using ComposeUI.Messaging.Client.Transport.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace ComposeUI.Example.WPFDataGrid.TestApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceCollection _serviceCollection = new();

    /// <summary>
    /// Url to connect
    /// </summary>
    public static Uri WebsocketURI { get; } = new("ws://localhost:5098/ws");

    /// <summary>
    /// Overriding Statup so we can do DI.
    /// </summary>
    /// <param name="e"></param>
    protected async override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ILoggerFactory loggerFactory = new LoggerFactory();

        var serilogger = new LoggerConfiguration()
            .WriteTo.File(string.Format("{0}/log.log", Directory.GetCurrentDirectory()))
            .CreateLogger();

        _serviceCollection.AddLogging(builder =>
        {
            builder.AddSerilog(serilogger);
        });

        var messageRouter = MessageRouter.Create(
            mr => mr.UseWebSocket(
                new MessageRouterWebSocketOptions
                {
                    Uri = WebsocketURI
                }));
        _serviceCollection.AddSingleton<IMessageRouter>(messageRouter);
        _serviceCollection.AddTransient(typeof(DataGridView));
        var provider = _serviceCollection.BuildServiceProvider();
        var dataGridView = provider.GetRequiredService<DataGridView>();

        dataGridView?.Show();
    }
}

