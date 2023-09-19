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


using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
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
        private readonly MockAppDirectory _mockAppDirectory = new();
        private readonly MockModuleLoader _mockModuleLoader = new();
        private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new IIntentMetadataJsonConverter(), new IAppMetadataJsonConverter(), new AppMetadataJsonConverter(), new IntentResultJsonConverter() }
        };

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

                    services.AddSingleton<IAppDirectory>(_mockAppDirectory);
                    services.AddTransient<IModuleLoader>(s => _mockModuleLoader);

                    services.AddFdc3DesktopAgent(
                        fdc3 => fdc3.Configure(
                            builder =>
                            {
                                builder.ChannelId =
                                    TestChannel; 
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

        [Fact]
        public async Task FindIntentReturnsAppIntent()
        {
            var apps = await _mockAppDirectory.GetApps();
            var app = await _mockAppDirectory.GetApp("appId4");

            app.Should().NotBeNull();

            var request = new FindIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = app!.Interop!.Intents!.ListensFor!.First().Key,
            };

            var response = new FindIntentResponse()
            {
                AppIntent = new AppIntent(app!.Interop!.Intents!.ListensFor!.First().Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: app!.Interop!.Intents!.ListensFor!.First().Value.ResultType),
                        new AppMetadata(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //appId6
                    })
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
        {
            var app = await _mockAppDirectory.GetApp("appId7");

            app.Should().NotBeNull();

            var response = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntent, null);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentReturnsNoAppsFound()
        {
            var app = await _mockAppDirectory.GetApp("appId7");

            app.Should().NotBeNull();

            var request = new FindIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "noAppShouldReturnIntent",
            };

            var response = new FindIntentResponse()
            {
                Error = ResolveError.NoAppsFound
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentReturnsIntentDeliveryFailureBecauseOfItFoundMultipleAppIntent()
        {
            var app = await _mockAppDirectory.GetApp("appId7");

            app.Should().NotBeNull();

            var request = new FindIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = app!.Interop!.Intents!.ListensFor!.ElementAt(1).Key,
            };

            var response = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentsByContextReturnsAppIntent()
        {
            var apps = await _mockAppDirectory.GetApps();

            var request = new FindIntentsByContextRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Context = new Context("context2"),
                ResultType = "resultType"
            };

            var response = new FindIntentsByContextResponse()
            {
                AppIntents = new[]
                    {
                        new AppIntent(
                            apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                                new AppMetadata(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType) //appId6
                            })
                    }
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntentsByContext, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentsByContextReturnsMultipleAppIntent()
        {
            var apps = await _mockAppDirectory.GetApps();

            var request = new FindIntentsByContextRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Context = new Context("context9"),
                ResultType = "resultWrongApp"
            };

            var response = new FindIntentsByContextResponse()
            {
                AppIntents = new[]
                    {
                        new AppIntent(
                            apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(apps.ElementAt(8).AppId, name: apps.ElementAt(8).Name, resultType: apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(apps.ElementAt(9).AppId, name: apps.ElementAt(9).Name, resultType: apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            })
                    }
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntentsByContext, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentsByContextReturnsIntentDeliveryFailureBecauseOfTheRequest()
        {
            var response = new FindIntentsByContextResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntentsByContext, null);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task FindIntentsByContextReturnsNoAppsFound()
        {
            var request = new FindIntentsByContextRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Context = new Context("context2"),
                ResultType = "noAppShouldReturn"
            };

            var response = new FindIntentsByContextResponse()
            {
                Error = ResolveError.NoAppsFound
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.FindIntentsByContext, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<FindIntentsByContextResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RaiseIntentReturnsIntentDeliveryFailureBecauseOfTheRequest()
        {
            var response = new RaiseIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, null);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RaiseIntentReturnsErrorAsMessageContainsError()
        {
            var request = new RaiseIntentRequest()
            {
                InstanceId = "dummy",
                Error = "dummyError"
            };
            var response = new RaiseIntentResponse()
            {
                Error = "dummyError"
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RaiseIntentReturnsNoAppsFound()
        {
            var request = new RaiseIntentRequest()
            {
                InstanceId = "dummy",
                Intent = "noIntentShouldHandle",
                Context = new Context("fdc3.nothing")
            };
            var response = new RaiseIntentResponse()
            {
                Error = ResolveError.NoAppsFound
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RaiseIntentReturnsIntentDeliveryFailureAsMultipleAppIntentFound()
        {
            var app = await _mockAppDirectory.GetApp("appId7");
            var request = new RaiseIntentRequest()
            {
                InstanceId = "dummy",
                Intent = app!.Interop!.Intents!.ListensFor!.ElementAt(1).Key,
                Context = new Context("fdc3.nothing")
            };
            var response = new RaiseIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RaiseIntentReturnsAppIntentWithOneApp()
        {
            var app = await _mockAppDirectory.GetApp("appId4");
            var request = new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = app!.Interop!.Intents!.ListensFor!.ElementAt(1).Key,
                Context = new Context(app!.Interop!.Intents!.ListensFor!.ElementAt(1).Value.Contexts.First())
            };
            var response = new RaiseIntentResponse()
            {
                Intent = app!.Interop!.Intents!.ListensFor!.ElementAt(1).Key,
                AppMetadatas = new AppMetadata[]
                {
                    new(app.AppId, name: app.Name, resultType: app!.Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
                }
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
            var requests = _mockModuleLoader.StartRequests.Where(application => application.Manifest.Id == "appId4");
            requests.Should().HaveCount(1);
        }

        [Fact]
        public async Task RaiseIntentReturnsAppIntentWithMultipleApps()
        {
            var apps = await _mockAppDirectory.GetApps();
            var app = await _mockAppDirectory.GetApp("appId4");
            var request = new RaiseIntentRequest()
            {
                InstanceId = "dummyOneAppIntent",
                Intent = app!.Interop!.Intents!.ListensFor!.First().Key,
                Context = new Context(app!.Interop!.Intents!.ListensFor!.First().Value.Contexts.First())
            };
            var response = new RaiseIntentResponse()
            {
                Intent = app!.Interop!.Intents!.ListensFor!.First().Key,
                AppMetadatas = new AppMetadata[]
                {
                    new(app.AppId, name: app.Name, resultType: app!.Interop!.Intents!.ListensFor!.First().Value.ResultType),
                    new(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.First().Value.ResultType),
                    new(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
                }
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.RaiseIntent, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<RaiseIntentResponse>(_options);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task StoreIntentResultReturnsIntentDeliveryFailureAsRequestNull()
        {
            var response = new StoreIntentResultResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.SendIntentResult, null);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task StoreIntentResultReturnsIntentDeliveryFailureAsRequestNotContainsInformation()
        {
            var request = new StoreIntentResultRequest();
            var response = new StoreIntentResultResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task StoreIntentResultReturnsSuccessFully()
        {
            var request = new StoreIntentResultRequest()
            {
                Intent = "testIntent",
                TargetInstanceId = Guid.NewGuid().ToString(),
                IntentResult = new Context("testContextType")
            };
            var response = new StoreIntentResultResponse()
            {
                Stored = true
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<StoreIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestNull()
        {
            var response = new GetIntentResultResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.GetIntentResult, null);
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetIntentResultReturnsIntentDeliveryFailureAsRequestNotContainsInformation()
        {
            var request = new GetIntentResultRequest();
            var response = new GetIntentResultResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetIntentResultReturnsIntentDeliveryFailureAsNoIntentResultFound()
        {
            var request = new GetIntentResultRequest()
            {
                Intent = "testIntent",
                Source = new AppIdentifier("appId1", Guid.NewGuid().ToString())
            };
            var response = new GetIntentResultResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task GetIntentResultReturnsSuccessFully()
        {
            var originInstanceId = Guid.NewGuid().ToString();
            var context = new Context("dummyContextType");
            var storeIntentRequest = new StoreIntentResultRequest()
            {
                Intent = "testIntent",
                IntentResult = context,
                TargetInstanceId = originInstanceId
            };

            await _messageRouter.InvokeAsync(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options));
            
            var request = new GetIntentResultRequest()
            {
                Intent = "testIntent",
                Source = new AppIdentifier("testApp", originInstanceId)
            };

            var response = new GetIntentResultResponse()
            {
                IntentResult = context
            };

            var resultBuffer = await _messageRouter.InvokeAsync(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(request, _options));
            resultBuffer.Should().NotBeNull();
            var result = resultBuffer!.ReadJson<GetIntentResultResponse>(_options);
            result!.Should().BeEquivalentTo(response);
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