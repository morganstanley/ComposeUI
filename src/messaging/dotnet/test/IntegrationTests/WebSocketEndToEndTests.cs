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

using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;

namespace MorganStanley.ComposeUI.Messaging;

public class WebSocketEndToEndTests : EndToEndTestsBase
{
    protected override void ConfigureServer(MessageRouterServerBuilder serverBuilder)
    {
        serverBuilder.UseWebSockets(
            opt =>
            {
                opt.RootPath = _webSocketUri.AbsolutePath;
                opt.Port = _webSocketUri.Port;
            });
    }

    protected override IMessageRouter CreateClient()
    {
        var services = new ServiceCollection()
            .AddMessageRouter(
                mr => mr
                    .UseWebSocket(
                        new MessageRouterWebSocketOptions
                        {
                            Uri = _webSocketUri
                        })
                    .UseAccessToken(AccessToken))
            .BuildServiceProvider();

        AddDisposable(services);

        return services.GetRequiredService<IMessageRouter>();
    }


    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
}