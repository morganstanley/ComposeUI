/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests
{
    public class EndToEndTests : IAsyncLifetime
    {
        private IHost _host;
        private IMessageRouter _messageRouter;
        private ServiceProvider _clientServices;
        private readonly Uri _webSocketUri = new("ws://localhost:7098/ws");
        private const string TestChannel = "testChannel";
        private readonly UserChannelTopics _topics = new UserChannelTopics(TestChannel);
        private const string AccessToken = "token";

        public async Task InitializeAsync()
        {
            // Create the backend side
            IHostBuilder builder = new HostBuilder();
            builder.ConfigureServices(
                services =>
                {
                    services.AddMessageRouterServer(
                        s => s.UseWebSockets(
                            opt =>
                            {
                                opt.RootPath = _webSocketUri.AbsolutePath;
                                opt.Port = _webSocketUri.Port;
                            }));
                    services.AddMessageRouter(mr => mr.UseServer());
                    services.AddFdc3DesktopAgent(
                        fdc3 => fdc3.Configure(
                            builder =>
                            {
                                builder.ChannelId =
                                    TestChannel; //DesktopAgent will call the `AddUserChannel` method, as this property is set for the `_options` field;
                            }));
                });
            _host = builder.Build();
            await _host.StartAsync();

            // Create a client acting in place of an application
            _clientServices = new ServiceCollection()
                .AddMessageRouter(
                    mr => mr.UseWebSocket(
                        new MessageRouterWebSocketOptions
                        {
                            Uri = _webSocketUri
                        }))
                .BuildServiceProvider();

            _messageRouter = _clientServices.GetRequiredService<IMessageRouter>();
        }

        public async Task DisposeAsync()
        {
            await _clientServices.DisposeAsync();
            await _host.StopAsync();
            _host.Dispose();
        }

        [Fact]
        public async void GetCurrentContextReturnsNullBeforeBroadcast()
        {
            var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, EmptyContextType);
            resultBuffer.Should().BeNull();
        }

        [Fact]
        public async void GetCurrentContextReturnsAfterBroadcast()
        {
            var ctx = GetContext();

            await _messageRouter.PublishAsync(_topics.Broadcast, ctx);

            await Task.Delay(100);

            var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, ContextType);

            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<Contact>();

            result.Should().BeEquivalentTo(ctx.ReadJson<Contact>());
        }

        [Fact]
        public async void GetCurrentContextReturnsAfterBroadcastWithNoType()
        {
            var ctx = GetContext();

            await _messageRouter.PublishAsync(_topics.Broadcast, ctx);

            await Task.Delay(100);

            var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, EmptyContextType);

            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<Contact>();

            result.Should().BeEquivalentTo(ctx.ReadJson<Contact>());
        }

        [Fact]
        public async void DifferentGetCurrentContextReturnsNullAfterBroadcast()
        {
            var ctx = GetContext();
            await _messageRouter.PublishAsync(_topics.Broadcast, ctx);
            await Task.Delay(100);
            var resultBuffer = await _messageRouter.InvokeAsync(_topics.GetCurrentContext, OtherContextType);
            resultBuffer.Should().BeNull();
        }

        [Fact]
        public async void FindUserChannelReturnsFoundTrueForExistingChannel()
        {
            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindChannel, FindRequest);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindChannelResponse>();
            result.Should().BeEquivalentTo(FindChannelResponse.Success);
        }

        [Fact]
        public async void FindUserChannelReturnsNoChannelFoundForNonExistingChannel()
        {
            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindChannel, FindNonExistingRequest);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindChannelResponse>();
            result.Should().BeEquivalentTo(FindChannelResponse.Failure(ChannelError.NoChannelFound));
        }

        private int _counter = 0;

        private MessageBuffer EmptyContextType => MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest());

        private MessageBuffer ContextType =>
            MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest {ContextType = new Contact().Type});

        private MessageBuffer OtherContextType =>
            MessageBuffer.Factory.CreateJson(new GetCurrentContextRequest {ContextType = new Email(null).Type});

        private MessageBuffer GetContext() => MessageBuffer.Factory.CreateJson(
            new Contact(
                new ContactID() {Email = $"test{_counter}@test.org", FdsId = $"test{_counter++}"},
                "Testy Tester"));

        private MessageBuffer FindRequest => MessageBuffer.Factory.CreateJson(
            new FindChannelRequest {ChannelId = TestChannel, ChannelType = ChannelType.User});

        private MessageBuffer FindNonExistingRequest => MessageBuffer.Factory.CreateJson(
            new FindChannelRequest {ChannelId = "nonexisting", ChannelType = ChannelType.User});
    }
}