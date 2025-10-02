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

using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var serviceCollection = new ServiceCollection()
            .AddMessageRouter(m =>
            {
                m.UseWebSocketFromEnvironment();
            })
            .AddMessageRouterMessagingAdapter()
            .AddFdc3DesktopAgentClient()
            .AddLogging(l => l.AddFile($".\\log.txt", LogLevel.Trace));

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var desktopAgentClient = serviceProvider.GetRequiredService<IDesktopAgent>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            if (desktopAgentClient == null)
            {
                throw new Exception("Failed to get IDesktopAgent from service provider...");
            }

            await desktopAgentClient.JoinUserChannel("fdc3.channel.1");

            var currentChannel = await desktopAgentClient.GetCurrentChannel();

            if (currentChannel == null)
            {
                throw new Exception("Failed to join to a channel...");
            }

            logger.LogInformation("Joined to a channel...");

            //Testing the broadcast
            await desktopAgentClient.Broadcast(new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name"));
            logger.LogInformation("Broadcasted an instrument to the current channel...");

            var appChannel = await desktopAgentClient.GetOrCreateChannel("app-channel-1");
            logger.LogInformation("Got or created app channel...");

            if (appChannel == null)
            {
                throw new Exception("Failed to get or create app channel...");
            }

            var listener = await appChannel.AddContextListener<Instrument>("fdc3.instrument", (context, metadata) =>
            {
                logger.LogInformation($"Received context in app channel: {context?.Name} - {context?.ID?.Ticker}");
                Console.WriteLine($"Received context in app channel: {context?.Name} - {context?.ID?.Ticker}");
            });

            await appChannel.Broadcast(new Instrument(new InstrumentID { Ticker = $"test-instrument-2" }, "test-name2"));
            logger.LogInformation("Broadcasted an instrument to the app channel...");

            var intentListener = await desktopAgentClient.AddIntentListener<Instrument>("ViewInstrument", (context, metadata) =>
            {
                logger.LogInformation($"Intent received: {context?.Name} - {context?.ID?.Ticker}");
                Console.WriteLine($"Intent received: {context?.Name} - {context?.ID?.Ticker}");
                return Task.FromResult<IIntentResult>(currentChannel);
            });

            var instances = await desktopAgentClient.FindInstances(new AppIdentifier { AppId = "appId1" });
            var instance = instances.First();

            logger.LogDebug($"Initiator identified: {instance.AppId}; {instance.InstanceId}, but retrieved instances were : {instances.Count()}...");

            var intentResolution = await desktopAgentClient.RaiseIntentForContext(new Valuation("USD", 400, 1, "02/10/2025", "10/10/2025", "USD", "USD"), instance);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error during DesktopAgentClient test...");
        }

        await Task.Delay(2000);
        logger.LogInformation("DesktopAgent is tested...");

        Console.WriteLine("DesktopAgent is tested...");
        Console.ReadLine();
    }
}