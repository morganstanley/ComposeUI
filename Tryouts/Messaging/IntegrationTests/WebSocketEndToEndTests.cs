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

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;

namespace MorganStanley.ComposeUI.Messaging;

public class WebSocketEndToEndTests : IAsyncLifetime
{
    [Fact]
    public async Task Client_can_connect()
    {
        await using var client = CreateClient();
        await client.ConnectAsync();
    }

    [Fact]
    public async Task Client_can_subscribe_and_receive_messages()
    {
        await using var publisher = CreateClient();
        await using var subscriber = CreateClient();
        var observerMock = new Mock<IObserver<TopicMessage>>();
        var receivedMessages = new List<TopicMessage>();
        observerMock.Setup(x => x.OnNext(Capture.In(receivedMessages)));

        await subscriber.SubscribeAsync("test-topic", observerMock.Object);
        await Task.Delay(100);

        var publishedPayload = new TestPayload
        {
            IntProperty = 0x10203040,
            StringProperty = "Compose UI 🔥"
        };

        await publisher.PublishAsync(
            "test-topic",
            MessageBuffer.Create(JsonSerializer.SerializeToUtf8Bytes(publishedPayload)));

        await Task.Delay(100);

        var receivedPayload = JsonSerializer.Deserialize<TestPayload>(receivedMessages.Single().Payload!.GetSpan());

        receivedPayload.Should().BeEquivalentTo(publishedPayload);
    }

    [Fact]
    public async Task Client_can_register_itself_as_a_service()
    {
        await using var client = CreateClient();
        await client.RegisterServiceAsync("test-service", (name, payload, context) => default);
        await client.UnregisterServiceAsync("test-service");
    }

    [Fact]
    public async Task Client_can_invoke_a_registered_service()
    {
        await using var service = CreateClient();

        var handlerMock = new Mock<MessageHandler>();

        handlerMock
            .Setup(_ => _.Invoke("test-service", It.IsAny<MessageBuffer?>(), It.IsAny<MessageContext>()))
            .Returns(new ValueTask<MessageBuffer?>(MessageBuffer.Create("test-response")));

        await service.RegisterServiceAsync("test-service", handlerMock.Object);

        await using var client = CreateClient();

        var response = await client.InvokeAsync("test-service", "test-request");

        response.Should().BeEquivalentTo("test-response");

        handlerMock.Verify(
            _ => _.Invoke(
                "test-service",
                It.Is<MessageBuffer>(buf => buf.GetString() == "test-request"),
                It.IsAny<MessageContext>()));

        await service.UnregisterServiceAsync("test-service");
    }

    [Fact]
    public async Task Client_can_invoke_another_client_by_id_as_long_as_it_is_registered()
    {
        await using var callee = CreateClient();
        await using var caller = CreateClient();

        var handlerMock = new Mock<MessageHandler>();

        handlerMock.Setup(_ => _.Invoke(It.IsAny<string>(), It.IsAny<MessageBuffer?>(), It.IsAny<MessageContext>()))
            .ReturnsAsync(
                (string endpoint, MessageBuffer? payload, MessageContext context) =>
                    MessageBuffer.Create("test-response"));

        await callee.RegisterEndpointAsync("test-endpoint", handlerMock.Object);

        var response = await caller.InvokeAsync(
            "test-endpoint",
            "test-request",
            new InvokeOptions
            {
                Scope = MessageScope.FromClientId(callee.ClientId!)
            });

        response.Should().BeEquivalentTo("test-response");

        handlerMock.Verify(
            _ => _.Invoke(
                "test-endpoint",
                It.Is<MessageBuffer>(buf => buf.GetString() == "test-request"),
                It.IsAny<MessageContext>()));

        await callee.UnregisterEndpointAsync("test-endpoint");

        await Assert.ThrowsAsync<UnknownEndpointException>(
            async () => await caller.InvokeAsync(
                "test-endpoint",
                "test-request",
                new InvokeOptions
                {
                    Scope = MessageScope.FromClientId(callee.ClientId!)
                }));
    }

    public async Task InitializeAsync()
    {
        IHostBuilder builder = new HostBuilder();

        builder.ConfigureServices(
            services => services.AddMessageRouterServer(
                mr => mr.UseWebSockets(
                        opt =>
                        {
                            opt.RootPath = _webSocketUri.AbsolutePath;
                            opt.Port = _webSocketUri.Port;
                        })
                    .UseAccessTokenValidator(
                        new Action<string, string?>(
                            (clientId, token) =>
                            {
                                if (token != AccessToken)
                                    throw new InvalidOperationException("Invalid access token");
                            }))));

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
    }

    private IHost _host = null!;
    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
    private const string AccessToken = "token";

    private IMessageRouter CreateClient()
    {
        return new ServiceCollection()
            .AddMessageRouter(
                mr => mr
                    .UseWebSocket(
                        new MessageRouterWebSocketOptions
                        {
                            Uri = _webSocketUri
                        })
                    .UseAccessToken(AccessToken))
            .BuildServiceProvider()
            .GetRequiredService<IMessageRouter>();
    }

    private class TestPayload
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
    }
}
