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
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestUtils;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.AppDirectory;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3DesktopAgentTests
{
    private static readonly Mock<IMessageRouter> MockMessageRouter = new();
    private static readonly MockAppDirectory MockAppDirectory = new();
    private static readonly MockModuleLoader MockModuleLoader = new();

    private Fdc3DesktopAgent _fdc3 = new(
        MockAppDirectory,
        MockModuleLoader,
        new Fdc3DesktopAgentOptions(),
        MockMessageRouter.Object,
        NullLoggerFactory.Instance);

    private const string TestChannel = "testChannel";

    private JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new IIntentMetadataJsonConverter(), new IAppMetadataJsonConverter(), new AppMetadataJsonConverter(), new IntentResultJsonConverter() }
    };


    [Fact]
    public async void UserChannelAddedCanBeFound()
    {
        await _fdc3.AddUserChannel(TestChannel);

        var result = await _fdc3.FindChannel(Fdc3Topic.FindChannel, FindTestChannel, new MessageContext());

        result.Should().NotBeNull();
        result.ReadJson<FindChannelResponse>().Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_AppIdentifier()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = intentMetadata.Name,
                Context = new Context(intentMetadata.Contexts.First()),
                AppIdentifier = new AppIdentifier(app.AppId)
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType)
            });

        var requests = MockModuleLoader.StartRequests.ToList().Where(request => request.Manifest.Id == app.AppId);
        requests.Should().NotBeNull();
        requests.Should().HaveCount(1);
        await MockModuleLoader.StopModule(new StopRequest(requests.First().InstanceId));
    }

    [Fact]
    public async Task RaiseIntent_fails_by_request_delivery_error()
    {
        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            null,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_returns_one_app_by_Context()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3);
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.ElementAt(1);

        var request = new RaiseIntentRequest()
        {
            InstanceId = Guid.NewGuid().ToString(),
            Intent = intentMetadata.Name,
            Context = new Context(intentMetadata.Contexts.First())
        };
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(request);

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().HaveCount(1);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType)
            });

        var requests = MockModuleLoader.StartRequests.Where(request => request.Manifest.Id == app.AppId);
        requests.Should().NotBeNull();
        requests.Should().HaveCount(1);
        await MockModuleLoader.StopModule(new StopRequest(requests.First().InstanceId));
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context()
    {
        var apps = await MockAppDirectory.GetApps();
        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = instanceId,
                Intent = intentMetadata.Name, //intentMetadata4
                Context = new Context(intentMetadata.Contexts.First())
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType),
                new(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.First().Value.ResultType),
                new(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
            });

        MockMessageRouter.Verify(
            _ => _.RegisterServiceAsync(Fdc3Topic.RaiseIntentResolution(instanceId), It.IsAny<MessageHandler>(), It.IsAny<EndpointDescriptor>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task RaiseIntent_returns_multiple_apps_by_Context_if_fdc3_nothing()
    {
        var apps = await MockAppDirectory.GetApps();
        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //intentMetadata4
        var instanceId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = instanceId,
                Intent = intentMetadata.Name, //intentMetadata4
                Context = new Context("fdc3.nothing")
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().HaveCount(3);
        result!.ReadJson<RaiseIntentResponse>(_options)!.AppMetadatas.Should().BeEquivalentTo(
            new List<AppMetadata>()
            {
                new(app.AppId, name: app.Name, resultType: intentMetadata.ResultType),
                new(apps.ElementAt(4).AppId, name: apps.ElementAt(4).Name, resultType: apps.ElementAt(4).Interop!.Intents!.ListensFor!.First().Value.ResultType),
                new(apps.ElementAt(5).AppId, name: apps.ElementAt(5).Name, resultType: apps.ElementAt(5).Interop!.Intents!.ListensFor!.ElementAt(1).Value.ResultType)
            });

        MockMessageRouter.Verify(
            _ => _.RegisterServiceAsync(Fdc3Topic.RaiseIntentResolution(instanceId), It.IsAny<MessageHandler>(), It.IsAny<EndpointDescriptor>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_AppIdentifier()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "testIntent",
                Context = new Context("contextType"),
                AppIdentifier = new AppIdentifier("noAppShouldReturn")
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Context()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3);
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First();

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = intentMetadata.Name,
                Context = new Context("noAppShouldReturn")
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_no_apps_found_by_Intent()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(3); //appId4
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.First(); //context2

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "noAppShouldReturn",
                Context = new Context(intentMetadata.Contexts!.First())
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_multiple_IAppIntents_found()
    {
        var apps = await MockAppDirectory.GetApps();

        var app = apps.ElementAt(6); //appId7
        var intentMetadata = app.Interop!.Intents!.ListensFor!.Values.ElementAt(1); //intentMetadata8

        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = intentMetadata.Name,
                Context = new Context(intentMetadata.Contexts.First()),
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntent_fails_as_request_specifies_error()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "noAppShouldReturn",
                Context = new Context("contextType"),
                Error = "Some weird error"
            });

        var result = await _fdc3.RaiseIntent(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be("Some weird error");
    }

    //RaiseIntentById service will execute when the user either selected an app from resolverui or there is just one app that previosuly was allowable by the RaiseIntent service.
    [Fact]
    public async Task RaiseIntentByIdService_fails_as_request_delivery_error()
    {
        var result = await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntentResolution("dummyId"),
            null,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.IntentDeliveryFailed);
    }

    [Fact]
    public async Task RaiseIntentByIdService_fails_as_the_selected_app_unreachable()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "intentMetadata1",
                Context = new Context("context1"),
                AppIdentifier = new AppIdentifier("appId2", Guid.NewGuid().ToString())
            });

        var result = await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntentResolution("dummyId"),
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();
        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.TargetInstanceUnavailable);
    }

    [Fact]
    public async Task RaiseIntentByIdService_fails_as_request_specifies_error()
    {
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = "testIntent",
                AppIdentifier = new AppIdentifier("appId1"),
                Context = new Context("contextType"),
                Error = "Some weird error"
            });

        var result = await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntent,
            raiseIntentRequest,
            new MessageContext());

        result!.ReadJson<RaiseIntentResponse>(_options)!.Error.Should().Be(ResolveError.UserCancelledResolution);
    }

    [Fact]
    public async Task RaiseIntentByIdService_raises_intent_resolution_to_a_running_app_instance()
    {
        await _fdc3.SubscribeAsync();

        var originId = Guid.NewGuid().ToString();

        //Starting an application
        await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntentResolution(originId.ToString()),
            MessageBuffer.Factory.CreateJson(
                new RaiseIntentRequest()
                {
                    InstanceId = originId,
                    Intent = "intentMetadata1",
                    Context = new Context("context1"),
                    AppIdentifier = new AppIdentifier("appId1")
                }),
            new MessageContext());

        //Find the started application's instanceId
        var findIntentResult = await _fdc3.FindIntent(
            "dummy",
            MessageBuffer.Factory.CreateJson(
                new FindIntentRequest()
                {
                    InstanceId = "dummy",
                    Intent = "intentMetadata1",
                    Context = new Context("context1")
                }),
            new MessageContext());

        findIntentResult!.ReadJson<FindIntentResponse>(_options)!.AppIntent!.Apps.Should().HaveCount(2);
        var instanceId = findIntentResult!.ReadJson<FindIntentResponse>(_options)!.AppIntent!.Apps.ElementAt(1).InstanceId;
        instanceId.Should().NotBeNull();

        //Test
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = instanceId, // for the test's sake i will use the same instanceId so the originating app will be the same which resolves the intent.
                Intent = "intentMetadata1",
                Context = new Context("context1"),
                AppIdentifier = new AppIdentifier("appId1", instanceId)
            });

        var result = await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntentResolution(originId.ToString()),
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();

        MockMessageRouter.Verify(
            _ =>
                _.PublishAsync(
                    Fdc3Topic.RaiseIntentResolution("intentMetadata1", instanceId!),
                    It.IsAny<MessageBuffer>(),
                    default,
                    CancellationToken.None),
            Times.Once);

        var requests = MockModuleLoader.StartRequests.Where(request => request.Manifest.Id == "appId1");
        requests.Should().NotBeNull();
        requests.Should().HaveCount(1);
        await MockModuleLoader.StopModule(new StopRequest(requests.First().InstanceId));
    }

    [Fact]
    public async Task RaiseIntentByIdService_raises_an_application_start_request_and_saves_the_context()
    {
        var originId = Guid.NewGuid().ToString();
        var raiseIntentRequest = MessageBuffer.Factory.CreateJson(
            new RaiseIntentRequest()
            {
                InstanceId = originId,
                Intent = "intentMetadata2",
                Context = new Context("dummyContext"),
                AppIdentifier = new AppIdentifier("appId2")
            });

        var result = await _fdc3.RaiseIntentById(
            Fdc3Topic.RaiseIntentResolution(originId.ToString()),
            raiseIntentRequest,
            new MessageContext());

        result.Should().NotBeNull();

        var requests = MockModuleLoader.StartRequests.Where(request => request.Manifest.Id == "appId2");
        requests.Should().NotBeNull();
        requests.Should().HaveCount(1);
        await MockModuleLoader.StopModule(new StopRequest(requests.First().InstanceId));
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.StoreIntentResult("dummy", null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_TargetInstanceId_is_null()
    {
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            Intent = "dummy",
            IntentResult = new Context("dummyType"),
        };
        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_Intent_is_null()
    {
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            IntentResult = new Context("dummyType"),
            TargetInstanceId = "dummyTarget"
        };
        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_fails_due_IntentResult_is_null()
    {
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            Intent = "dummy",
            TargetInstanceId = "dummyTarget"
        };
        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed, Stored = false });
    }

    [Fact]
    public async Task StoreIntentResult_succeeds()
    {
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            Intent = "dummy",
            IntentResult = new Context("dummyType"),
            TargetInstanceId = "dummyTarget"
        };

        var result = await _fdc3.StoreIntentResult(Fdc3Topic.SendIntentResult, MessageBuffer.Factory.CreateJson(storeIntentRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(new StoreIntentResultResponse() { Stored = true });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_the_request()
    {
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, null, new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_Source_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest() { Intent = "dummy", Version = "1.0" }; //Version should be the Intent's schema version
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_InstanceId_is_null()
    {
        var getIntentResultRequest = new GetIntentResultRequest() { Intent = "dummy", Source = new AppIdentifier("appId"), Version = "1.0" };
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_fails_due_no_intent_found()
    {
        var getIntentResultRequest = new GetIntentResultRequest() { Intent = "dummy", Source = new AppIdentifier("appId", "test"), Version = "1.0" };
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(new GetIntentResultResponse() { Error = ResolveError.IntentDeliveryFailed });
    }

    [Fact]
    public async Task GetIntentResult_succeeds()
    {
        var storeIntentRequest = new StoreIntentResultRequest()
        {
            Intent = "dummy",
            IntentResult = new Context("dummyContextType"),
            TargetInstanceId = "testInstance"
        };

        var request = MessageBuffer.Factory.CreateJson(storeIntentRequest, _options);

        var storeResult = await _fdc3.StoreIntentResult(
            Fdc3Topic.SendIntentResult,
            request,
            new MessageContext());
        storeResult.Should().NotBeNull();
        storeResult!.ReadJson<StoreIntentResultResponse>(_options).Should().BeEquivalentTo(StoreIntentResultResponse.Success());

        var getIntentResultRequest = new GetIntentResultRequest() { Intent = "dummy", Source = new AppIdentifier("appId", "testInstance"), Version = "1.0" };
        var result = await _fdc3.GetIntentResult(Fdc3Topic.GetIntentResult, MessageBuffer.Factory.CreateJson(getIntentResultRequest, _options), new MessageContext());
        result.Should().NotBeNull();
        result!.ReadJson<GetIntentResultResponse>(_options).Should().BeEquivalentTo(GetIntentResultResponse.Success(new Context("dummyContextType")));
    }

    private MessageBuffer FindTestChannel => MessageBuffer.Factory.CreateJson(new FindChannelRequest() { ChannelId = "testChannel", ChannelType = ChannelType.User });


    [Theory]
    [ClassData(typeof(FindIntentTheoryData))]
    public async Task FindIntent_edge_case_tests(FindIntentTestCase testCase)
    {
        var request = MessageBuffer.Factory.CreateJson(testCase.Request, _options);
        var result = await _fdc3.FindIntent(Fdc3Topic.FindIntent, request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppCount > 0)
        {
            result!.ReadJson<FindIntentResponse>(_options)!.AppIntent!.Apps.Should().HaveCount(testCase.ExpectedAppCount);
        }

        result!.ReadJson<FindIntentResponse>(_options)!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    [Theory]
    [ClassData(typeof(FindIntentsByContextTheoryData))]
    public async Task FindIntentsByContext_edge_case_tests(FindIntentsByContextTestCase testCase)
    {
        var request = MessageBuffer.Factory.CreateJson(testCase.Request, _options);
        var result = await _fdc3.FindIntentsByContext(Fdc3Topic.FindIntentsByContext, request, new MessageContext());

        result.Should().NotBeNull();

        if (testCase.ExpectedAppIntentsCount > 0)
        {
            result!.ReadJson<FindIntentsByContextResponse>(_options)!.AppIntents!.Should().HaveCount(testCase.ExpectedAppIntentsCount);
        }

        result!.ReadJson<FindIntentsByContextResponse>(_options)!.Should().BeEquivalentTo(testCase.ExpectedResponse);
    }

    public class FindIntentsByContextTheoryData : TheoryData
    {
        private readonly IEnumerable<Fdc3App> _apps;
        public FindIntentsByContextTheoryData()
        {
            _apps = MockAppDirectory.Apps;

            // Returning one AppIntent with one app by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("contextCustom") //This relates to the appId4 only
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1),
                            new []
                            {
                                new AppMetadata(appId: _apps.ElementAt(3).AppId, name: _apps.ElementAt(3).Name, resultType: _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType)
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple app by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context2") //This relates to the appId4, appId5, appId6, 
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.First(), //it should be the same for all app - the IntentMetadata
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(3).AppId, name: _apps.ElementAt(3).Name, resultType: _apps.ElementAt(3)!.Interop!.Intents!.ListensFor!.Values.First().ResultType),
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType),
                                new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType)
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple appIntent by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context9") //This relates to the wrongappId9 and an another wrongAppId9 with 2 individual IntentMetadata
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                                new AppMetadata(_apps.ElementAt(10).AppId, name: _apps.ElementAt(10).Name, resultType: _apps.ElementAt(10)!.Interop!.Intents!.ListensFor!.Values.First().ResultType) //appId11
                            })
                    }
                },
                ExpectedAppIntentsCount = 2
            });

            // Returning error no apps found by just passing Context
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("noAppShouldReturn") // no app should have this context type
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.NoAppsFound
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning one AppIntent with one app by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context2"), //This relates to multiple appId
                    ResultType = "resultType<specified>"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First(), // it should just return appId5
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType)
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning one AppIntent with multiple apps by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context2"), //This relates to multiple appId
                    ResultType = "resultType"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                                new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType) //appId6
                            })
                    }
                },
                ExpectedAppIntentsCount = 1
            });

            // Returning multiple AppIntent by ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context9"), //This relates to multiple appId
                    ResultType = "resultWrongApp"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(), 
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            })
                    }
                },
                ExpectedAppIntentsCount = 2
            });

            // Returning no apps found error by using ResultType
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("context9"), //This relates to multiple appId
                    ResultType = "noAppShouldReturn"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.NoAppsFound
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning intent delivery error
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = null,
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    Error = ResolveError.IntentDeliveryFailed
                },
                ExpectedAppIntentsCount = 0
            });

            // Returning all the apps that are using the ResultType by adding fdc3.nothing.
            AddRow(new FindIntentsByContextTestCase()
            {
                Request = new FindIntentsByContextRequest()
                {
                    Context = new Context("fdc3.nothing"),
                    ResultType = "resultWrongApp"
                },
                ExpectedResponse = new FindIntentsByContextResponse()
                {
                    AppIntents = new[]
                    {
                        new AppIntent(
                            _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(8).AppId, name: _apps.ElementAt(8).Name, resultType: _apps.ElementAt(8)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First(),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.First().ResultType), //wrongAppId9
                            }),
                        new AppIntent(
                            _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1),
                            new []
                            {
                                new AppMetadata(_apps.ElementAt(9).AppId, name: _apps.ElementAt(9).Name, resultType: _apps.ElementAt(9)!.Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //wrongAppId9
                            }),
                    }
                },
                ExpectedAppIntentsCount = 3
            });
        }
    }

    public class FindIntentsByContextTestCase
    {
        public FindIntentsByContextRequest Request { get; set; }
        public FindIntentsByContextResponse ExpectedResponse { get; set; }
        public int ExpectedAppIntentsCount { get; set; }
    }

    private class FindIntentTheoryData : TheoryData
    {
        private readonly IEnumerable<Fdc3App> _apps;
        public FindIntentTheoryData()
        {
            _apps = MockAppDirectory.Apps;
            AddRow(GenerateTestCase(FindIntentTestCaseType.Context, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Context, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Intent, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.Intent, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ResultType, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ResultType, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ContextResultType, true));
            AddRow(GenerateTestCase(FindIntentTestCaseType.ContextResultType, false));
            AddRow(GenerateTestCase(FindIntentTestCaseType.NoAppsFound));
            AddRow(GenerateTestCase(FindIntentTestCaseType.RequestError));
            AddRow(GenerateTestCase(FindIntentTestCaseType.MultipleAppIntents));
        }

        private FindIntentTestCase GenerateTestCase(
            FindIntentTestCaseType findIntentTestCaseType,
            bool multipleResult = false)
        {
            FindIntentTestCase findIntentTestCase = null;

            switch (findIntentTestCaseType)
            {
                case FindIntentTestCaseType.Intent:
                    findIntentTestCase = SelectTestCaseForIntent(multipleResult);
                    break;

                case FindIntentTestCaseType.Context:
                    findIntentTestCase = SelectTestCaseForContext(multipleResult);
                    break;

                case FindIntentTestCaseType.ResultType:
                    findIntentTestCase = SelectTestCaseForResultType(multipleResult);
                    break;

                case FindIntentTestCaseType.ContextResultType:
                    findIntentTestCase = SelectTestCaseForContextAndResultType(multipleResult);
                    break;

                case FindIntentTestCaseType.RequestError:
                    findIntentTestCase = SelectTestCaseForRequestError();
                    break;

                case FindIntentTestCaseType.NoAppsFound:
                    findIntentTestCase = SelectTestCaseForNoAppsFoundError();
                    break;

                case FindIntentTestCaseType.MultipleAppIntents:
                    findIntentTestCase = SelectTestCaseForMultipleAppIntentsError();
                    break;
            }

            return findIntentTestCase!;
        }

        private FindIntentTestCase SelectTestCaseForMultipleAppIntentsError()
        {
            //As per the documentation : https://github.com/morganstanley/fdc3-dotnet/blob/main/src/Fdc3/IIntentMetadata.cs
            //name is unique for the intents, so it should be unique for every app, or the app should have the same intentMetadata?
            //if so we should return multiple appIntents and do not return error message for the client.
            //We have setup a test case for wrongappId9 which contains wrongly setted up intentMetadata.
            var app = _apps.ElementAt(6); //appId7
            var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1); //intentMetadata8

            var findIntentRequest = new FindIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = intentMetadata.Key,
            };

            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = findIntentRequest
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForNoAppsFoundError()
        {
            var app = _apps.ElementAt(1);
            var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

            var findIntentRequest = new FindIntentRequest()
            {
                InstanceId = Guid.NewGuid().ToString(),
                Intent = intentMetadata.Key,
                Context = new Context("noAppShouldBeReturned")
            };

            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.NoAppsFound
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = findIntentRequest
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForRequestError()
        {
            var findIntentResponse = new FindIntentResponse()
            {
                Error = ResolveError.IntentDeliveryFailed
            };

            var result = new FindIntentTestCase()
            {
                ExpectedAppCount = 0,
                ExpectedResponse = findIntentResponse,
                Request = null
            };

            return result;
        }

        private FindIntentTestCase SelectTestCaseForContextAndResultType(bool multipleResult)
        {
            FindIntentTestCase result;

            if (!multipleResult)
            {
                var app = _apps.ElementAt(6); //appId7
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key, //"intentMetadata7"
                    Context = new Context(intentMetadata.Value.Contexts.First()), //"context8"
                    ResultType = intentMetadata.Value.ResultType //"resultType2<specified2>"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(5); //appId6
                var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1);

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key, //intentMetadata4
                    Context = new Context(intentMetadata.Value.Contexts.First()), //"context2"
                    ResultType = intentMetadata.Value.ResultType // "resultType"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),

                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 2
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForResultType(bool multipleResult)
        {
            FindIntentTestCase result;

            if (!multipleResult)
            {
                var app = _apps.ElementAt(6); //appId7
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key,
                    ResultType = intentMetadata.Value.ResultType //"resultType2<specified2>"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(
                        intentMetadata.Value,
                        new[]
                        {
                            new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                        })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(5); //appId6
                var intentMetadata = app.Interop!.Intents!.ListensFor!.ElementAt(1);

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key, //intentMetadata4
                    ResultType = intentMetadata.Value.ResultType // "resultType"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),

                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 2
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForContext(bool multipleResult)
        {
            FindIntentTestCase result;
            if (!multipleResult)
            {
                var app = _apps.ElementAt(1);
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key,
                    Context = new Context(intentMetadata.Value.Contexts.First()), //"context1"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(3);
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key,
                    Context = new Context(intentMetadata.Value.Contexts.First()) //"context2"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5).Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //appId6
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 3
                };
            }

            return result!;
        }

        private FindIntentTestCase SelectTestCaseForIntent(bool multipleResult)
        {
            FindIntentTestCase result;
            if (!multipleResult)
            {
                var app = _apps.ElementAt(1); //appId2
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key, //"intentMetadata2"
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType)
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 1
                };
            }
            else
            {
                var app = _apps.ElementAt(3); //appId4
                var intentMetadata = app.Interop!.Intents!.ListensFor!.First();

                var findIntentRequest = new FindIntentRequest()
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    Intent = intentMetadata.Key,
                };

                var findIntentResponse = new FindIntentResponse()
                {
                    AppIntent = new AppIntent(intentMetadata.Value, new[]
                    {
                        new AppMetadata(app.AppId, name: app.Name, resultType: intentMetadata.Value.ResultType),
                        new AppMetadata(_apps.ElementAt(4).AppId, name: _apps.ElementAt(4).Name, resultType: _apps.ElementAt(4).Interop!.Intents!.ListensFor!.Values.First().ResultType), //appId5
                        new AppMetadata(_apps.ElementAt(5).AppId, name: _apps.ElementAt(5).Name, resultType: _apps.ElementAt(5).Interop!.Intents!.ListensFor!.Values.ElementAt(1).ResultType), //appId6
                    })
                };

                result = new()
                {
                    Request = findIntentRequest,
                    ExpectedResponse = findIntentResponse,
                    ExpectedAppCount = 3
                };
            }

            return result!;
        }

        private enum FindIntentTestCaseType
        {
            Context,
            ResultType,
            Intent,
            ContextResultType,
            RequestError,
            NoAppsFound,
            MultipleAppIntents,
        }
    }

    public class FindIntentTestCase
    {
        public FindIntentRequest Request { get; set; }
        public FindIntentResponse ExpectedResponse { get; set; }
        public int ExpectedAppCount { get; set; }
    }
}
