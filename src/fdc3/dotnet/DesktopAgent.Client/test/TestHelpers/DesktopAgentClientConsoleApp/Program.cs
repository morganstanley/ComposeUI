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

Console.WriteLine("Hello, World!");
var serviceCollection = new ServiceCollection()
    .AddMessageRouter(m =>
    {
        m.UseWebSocketFromEnvironment();
    })
    .AddMessageRouterMessagingAdapter()
    .AddFdc3DesktopAgentClient()
    .AddLogging(l => l.AddFile("./log.txt", LogLevel.Trace));

var serviceProvider = serviceCollection.BuildServiceProvider();

var desktopAgentClient = serviceProvider.GetRequiredService<IDesktopAgent>();

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

//Testing the broadcast
await desktopAgentClient.Broadcast(new Instrument(new InstrumentID { Ticker = "test-instrument" }, "test-name"));

var appChannel = await desktopAgentClient.GetOrCreateChannel("app-channel-1");

if (appChannel == null)
{
   throw new Exception("Failed to get or create app channel...");
}

var listener = await appChannel.AddContextListener<Instrument>("fdc3.instrument", (context, metadata) =>
{
    Console.WriteLine($"Received context in app channel: {context?.Name} - {context?.ID?.Ticker}");
});

await appChannel.Broadcast(new Instrument(new InstrumentID { Ticker = $"test-instrument-2" }, "test-name2"));

Console.WriteLine("DesktopAgent is tested...");
Console.ReadLine();
