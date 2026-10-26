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

using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.Messaging.Abstractions;
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


    [Fact]
    public async Task A_client_with_an_invalid_token_receives_the_error_of_the_validator_before_the_socket_closes()
    {
        var services = new ServiceCollection()
            .AddMessageRouter(
                mr => mr
                    .UseWebSocket(new MessageRouterWebSocketOptions {Uri = _webSocketUri})
                    .UseAccessToken("invalid-token"))
            .BuildServiceProvider();
        AddDisposable(services);
        await using var client = services.GetRequiredService<IMessageRouter>();

        var connect = async () => await client.ConnectAsync();

        var exception = await connect.Should().ThrowAsync<MessageRouterException>();
        exception.Which.Message.Should().Contain("Invalid access token");
    }

    [Fact]
    public async Task Server_closes_the_socket_with_policy_violation_if_the_first_message_is_not_Connect()
    {
        using var socket = new ClientWebSocket();
        socket.Options.Proxy = null; // the test talks to localhost, never through a proxy
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(_webSocketUri, timeout.Token);

        await socket.SendAsync(
            Encoding.UTF8.GetBytes("""{"type":"RegisterService","requestId":"1","endpoint":"test-service"}"""),
            WebSocketMessageType.Text,
            endOfMessage: true,
            timeout.Token);

        var buffer = new byte[1024];
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, timeout.Token);
        } while (result.MessageType != WebSocketMessageType.Close);

        result.CloseStatus.Should().Be(WebSocketCloseStatus.PolicyViolation);

        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
    }

    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
}