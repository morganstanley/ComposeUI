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

using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using Nito.AsyncEx;
using TaskExtensions = MorganStanley.ComposeUI.Testing.TaskExtensions;

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
        await TaskExtensions.WaitForBackgroundTasksAsync(DefaultTestTimeout);

        var publishedPayload = new TestPayload
        {
            IntProperty = 0x10203040,
            StringProperty = "ComposeUI 🔥"
        };

        await publisher.PublishAsync(
            "test-topic",
            MessageBuffer.Create(JsonSerializer.SerializeToUtf8Bytes(publishedPayload)));

        await Task.Delay(10); // TODO: Investigate why WaitForBackgroundTasksAsync is unreliable in this particular scenario

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

        await Assert.ThrowsAsync<MessageRouterException>(
            async () => await caller.InvokeAsync(
                "test-endpoint",
                "test-request",
                new InvokeOptions
                {
                    Scope = MessageScope.FromClientId(callee.ClientId!)
                }));
    }

    [Fact]
    public async Task Subscriber_can_invoke_a_service_without_deadlock()
    {
        await using var subscriber = CreateClient();
        await using var service = CreateClient();
        await using var publisher = CreateClient();
        await service.RegisterServiceAsync("test-service", new Mock<MessageHandler>().Object);
        var tcs = new TaskCompletionSource();

        await subscriber.SubscribeAsync(
            "test-topic",
            AsyncObserver.Create<TopicMessage>(
                new Func<TopicMessage, ValueTask>(
                    async msg =>
                    {
                        await subscriber.InvokeAsync("test-service");
                        tcs.SetResult();
                    })));

        await publisher.PublishAsync("test-topic");
        await tcs.Task;
    }

    [Fact]
    public async Task Subscriber_is_called_sequentially_without_concurrency()
    {
        await using var subscriber = CreateClient();
        await using var publisher = CreateClient();
        var semaphore = new AsyncSemaphore(1);

        var tcs = new TaskCompletionSource();

        await subscriber.SubscribeAsync(
            "test-topic",
            AsyncObserver.Create<TopicMessage>(
                async msg =>
                {
                    using (await semaphore.LockAsync(new CancellationTokenSource(TimeSpan.Zero).Token))
                    {
                        await TaskExtensions.WaitForBackgroundTasksAsync(DefaultTestTimeout);
                    }

                    if (msg.Payload?.GetString() == "done")
                    {
                        tcs.SetResult();
                    }
                }));

        for (var i = 0; i < 10; i++)
        {
            await publisher.PublishAsync("test-topic");
        }

        await publisher.PublishAsync("test-topic", "done");
        await tcs.Task;
    }

    [Fact]
    public async Task Endpoint_handler_can_invoke_a_service_without_deadlock()
    {
        await using var listener = CreateClient();
        await using var caller = CreateClient();
        await using var service = CreateClient();
        var tcs = new TaskCompletionSource();

        await listener.RegisterEndpointAsync(
            "test-endpoint",
            new MessageHandler(
                async (endpoint, payload, context) =>
                {
                    await listener.InvokeAsync("test-service");
                    tcs.SetResult();
                    return null;
                }));

        await service.RegisterServiceAsync("test-service", new Mock<MessageHandler>().Object);
        await caller.InvokeAsync(
            "test-endpoint",
            options: new InvokeOptions {Scope = MessageScope.FromClientId(listener.ClientId!)});
        await tcs.Task;
    }

    [Fact]
    public async Task Endpoint_handler_can_be_invoked_recursively_without_deadlock()
    {
        await using var serviceA = CreateClient();
        await using var serviceB = CreateClient();
        var tcs = new TaskCompletionSource();

        await serviceA.RegisterServiceAsync(
            "test-service-a",
            new MessageHandler(
                async (endpoint, payload, context) =>
                {
                    if (payload?.GetString() == "done")
                    {
                        tcs.SetResult();
                    }
                    else
                    {
                        await serviceA.InvokeAsync("test-service-b", "hello");
                    }

                    return null;
                }));

        await serviceB.RegisterServiceAsync(
            "test-service-b",
            new MessageHandler(
                async (endpoint, payload, context) =>
                {
                    await serviceB.InvokeAsync("test-service-a", "done");

                    return null;
                }));

        await serviceB.InvokeAsync("test-service-a");

        await tcs.Task;
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
                        (clientId, token) =>
                        {
                            if (token != AccessToken)
                                throw new InvalidOperationException("Invalid access token");
                        })));

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var disposable in _cleanup)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                disposable.Dispose();
            }
        }

        await _host.StopAsync();
        _host.Dispose();
    }

    public static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromSeconds(1);

    private IHost _host = null!;
    private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
    private const string AccessToken = "token";
    private readonly List<IDisposable> _cleanup = new();

    private IMessageRouter CreateClient()
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

        _cleanup.Add(services);

        return services.GetRequiredService<IMessageRouter>();
    }

    private class TestPayload
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
    }
}