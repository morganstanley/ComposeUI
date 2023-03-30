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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;

namespace ComposeUI.Example.DataService;

class Program
{
    public static Uri WebsocketUri = new("ws://localhost:5000/ws");

    public static async Task Main()
    {
        Console.WriteLine("Data Service");

        ServiceCollection serviceCollection = new();

        serviceCollection
            .AddMessageRouter(
                mr => mr.UseWebSocket(
                    new MessageRouterWebSocketOptions
                    {
                        Uri = WebsocketUri
                    }))
            .AddLogging(
                builder => builder.AddConsole())
            .AddSingleton<MonthlySalesDataService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var monthlySalesDataService = serviceProvider.GetRequiredService<MonthlySalesDataService>();
        await monthlySalesDataService.Start();

        var stopTaskSource = new TaskCompletionSource();

        Console.CancelKeyPress += (sender, args) =>
        {
            stopTaskSource.SetResult();
            args.Cancel = true;
        };

        await stopTaskSource.Task;

        Console.WriteLine("Exiting");

        await serviceProvider.DisposeAsync();

        Console.WriteLine("Done");
    }
}
