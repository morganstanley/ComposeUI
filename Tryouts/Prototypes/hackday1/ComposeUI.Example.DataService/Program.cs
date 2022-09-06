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

using ComposeUI.Messaging.Client;
using ComposeUI.Messaging.Client.Transport.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComposeUI.Example.DataService;

internal class DataService {
    private IServiceProvider? _serviceProvider;
    public static Uri WebsocketURI { get; set; } = new("ws://localhost:5000/ws");

    public static void Main()
    {
        Console.WriteLine("Data Service");

        IMessageRouter messageRouter = MessageRouter.Create(
            mr => mr.UseWebSocket(
                new MessageRouterWebSocketOptions
                {
                    Uri = WebsocketURI
                }));

        ServiceCollection serviceCollection = new();
        serviceCollection.AddLogging(
            builder => {
                builder
                    .AddFilter("Microsoft", LogLevel.Information)
                    .AddFilter("System", LogLevel.Information)
                    .AddFilter("LoggingConsoleApp.DataService", LogLevel.Information)
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });
        
        serviceCollection.AddSingleton<IMessageRouter>(messageRouter);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<Publisher>>();
        
        var publisher = new Publisher(messageRouter, logger);
        publisher.Subscribe();

        Console.ReadLine();
    }
}