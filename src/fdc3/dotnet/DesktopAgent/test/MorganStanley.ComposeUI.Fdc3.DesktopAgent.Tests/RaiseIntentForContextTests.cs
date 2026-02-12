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

using Finos.Fdc3;
using Finos.Fdc3.Context;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.ModuleLoader;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class RaiseIntentForContextTests : Fdc3DesktopAgentServiceTestsBase
{
    public RaiseIntentForContextTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task RaiseIntentForContext_returns_NoAppsFound()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = new Context("nosuchcontext").AsJson(),
        };

        var result = await Fdc3.RaiseIntentForContext(request, "nosuchcontext");

        result?.Response.Should().NotBeNull();
        result!.Response.Error.Should().Be(ResolveError.NoAppsFound);
    }


    [Fact]
    public async Task RaiseIntentForContext_with_multiple_possibilities_calls_ResolverUI()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        ResolverUICommunicator
            .Setup(x => x.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolverUIIntentResponse()
            {
                SelectedIntent = Intent2.Name
            });

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = MultipleContext.AsJson()
        };

        var result = await Fdc3.RaiseIntentForContext(request, MultipleContext.Type);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequestAsync(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RaiseIntentForContext_with_single_intent_but_multiple_apps_calls_ResolverUI()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        ResolverUICommunicator
            .Setup(x => x.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<ResolverUIIntentResponse?>(new ResolverUIIntentResponse()
            {
                SelectedIntent = IntentWithNoResult.Name
            }));

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = ContextType.Nothing.AsJson(),
        };

        var result = await Fdc3.RaiseIntentForContext(request, ContextTypes.Nothing);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequestAsync(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RaiseIntentForContext_with_multiple_intents_but_single_app_calls_ResolverUI()
    {
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        ResolverUICommunicator
            .Setup(x => x.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolverUIIntentResponse()
            {
                SelectedIntent = IntentWithNoResult.Name
            });

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = OnlyApp3Context.AsJson()
        };

        var result = await Fdc3.RaiseIntentForContext(request, OnlyApp3Context.Type);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIIntentRequestAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RaiseIntentForContext_logs_warning_when_the_raising_app_does_not_define_the_intent_in_the_AppDirectory_Raises_record()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = originFdc3InstanceId,
            Context = CurrencyContext.AsJson(),
        };

        // This should return an error anyway because no app can handle the intent
        _ = await Fdc3.RaiseIntentForContext(request, ContextTypes.Currency);

        Logger
            .Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Source app did not register its raiseable intent(s) for context: {ContextTypes.Currency} in the `raises` section of AppDirectory.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
    }
}
