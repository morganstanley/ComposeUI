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

using System.Reactive.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using Nito.AsyncEx;

namespace MorganStanley.ComposeUI.Messaging;

public abstract class EndToEndTestsBase : IAsyncLifetime
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

        await subscriber.SubscribeAsync(topic: "test-topic", observerMock.Object);
        await Task.Delay(TimeSpan.FromSeconds(2));

        var publishedPayload = new TestPayload
        {
            IntProperty = 0x10203040,
            StringProperty = "Compose UI 🔥"
        };

        await publisher.PublishAsync(
            topic: "test-topic",
            MessageBuffer.Create(JsonSerializer.SerializeToUtf8Bytes(publishedPayload)));

        await Task.Delay(TimeSpan.FromSeconds(2));

        var receivedPayload = JsonSerializer.Deserialize<TestPayload>(receivedMessages.Single().Payload!.GetSpan());

        receivedPayload.Should().BeEquivalentTo(publishedPayload);
    }

    [Fact]
    public async Task Client_can_register_itself_as_a_service()
    {
        await using var client = CreateClient();
        await client.RegisterServiceAsync(endpoint: "test-service", (name, payload, context) => default);
        await client.UnregisterServiceAsync("test-service");
    }

    [Fact]
    public async Task Client_can_invoke_a_registered_service()
    {
        await using var service = CreateClient();

        var handlerMock = new Mock<MessageHandler>();

        handlerMock
            .Setup(_ => _.Invoke("test-service", It.IsAny<MessageBuffer?>(), It.IsAny<MessageContext>()))
            .Returns(new ValueTask<IMessageBuffer?>(MessageBuffer.Create("test-response")));

        await service.RegisterServiceAsync(endpoint: "test-service", handlerMock.Object);

        await using var client = CreateClient();

        var response = await client.InvokeAsync(endpoint: "test-service", payload: "test-request");

        response.Should().BeEquivalentTo("test-response");

        handlerMock.Verify(
            _ => _.Invoke(
                "test-service",
                It.Is<MessageBuffer>(buf => buf.GetString() == "test-request"),
                It.IsAny<MessageContext>()));

        await service.UnregisterServiceAsync("test-service");
    }

    [Fact]
    public async Task Subscriber_can_invoke_a_service_without_deadlock()
    {
        await using var subscriber = CreateClient();
        await using var service = CreateClient();
        await using var publisher = CreateClient();
        await service.RegisterServiceAsync(endpoint: "test-service", new Mock<MessageHandler>().Object);
        var tcs = new TaskCompletionSource();

        await subscriber.SubscribeAsync(
            topic: "test-topic",
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
            topic: "test-topic",
            AsyncObserver.Create<TopicMessage>(
                async msg =>
                {
                    using (await semaphore.LockAsync(new CancellationTokenSource(TimeSpan.Zero).Token))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
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

        await publisher.PublishAsync(topic: "test-topic", payload: "done");
        await tcs.Task;
    }

    [Fact]
    public async Task Endpoint_handler_can_be_invoked_recursively_without_deadlock()
    {
        await using var serviceA = CreateClient();
        await using var serviceB = CreateClient();
        var tcs = new TaskCompletionSource();

        await serviceA.RegisterServiceAsync(
            endpoint: "test-service-a",
            new MessageHandler(
                async (endpoint, payload, context) =>
                {
                    if (payload?.GetString() == "done")
                    {
                        tcs.SetResult();
                    }
                    else
                    {
                        await serviceA.InvokeAsync(endpoint: "test-service-b", payload: "hello");
                    }

                    return null;
                }));

        await serviceB.RegisterServiceAsync(
            endpoint: "test-service-b",
            new MessageHandler(
                async (endpoint, payload, context) =>
                {
                    await serviceB.InvokeAsync(endpoint: "test-service-a", payload: "done");

                    return null;
                }));

        await serviceB.InvokeAsync("test-service-a");

        await tcs.Task;
    }

    public async Task InitializeAsync()
    {
        IHostBuilder builder = new HostBuilder();

        builder.ConfigureServices(
            services =>
            {
                services.AddMessageRouterServer(
                    server =>
                    {
                        server.UseAccessTokenValidator(
                            (clientId, token) =>
                            {
                                if (token != AccessToken)
                                    throw new InvalidOperationException("Invalid access token");
                            });
                        ConfigureServer(server);
                    });

                ConfigureServices(services);
            });

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

    protected IHost Host => _host ?? throw new InvalidOperationException("Host is not initialized yet.");

    protected void AddDisposable(IDisposable disposable)
    {
        _cleanup.Add(disposable);
    }

    protected const string AccessToken = "token";

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add any additional service registrations.
        // Don't call AddMessageRouterServer.
    }

    protected abstract void ConfigureServer(MessageRouterServerBuilder serverBuilder);

    protected abstract IMessageRouter CreateClient();

    private IHost _host = null!;

    private readonly List<IDisposable> _cleanup = new();

    private class TestPayload
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
    }
}