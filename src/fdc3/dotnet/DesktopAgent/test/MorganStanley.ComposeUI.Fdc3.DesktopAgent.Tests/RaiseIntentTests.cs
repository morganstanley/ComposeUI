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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class RaiseIntentTests : Fdc3DesktopAgentTestsBase
{
    public RaiseIntentTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task RaiseIntent_returns_NoAppsFound()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = "noAppShouldReturn",
            Context = SingleContext.AsJson()
        };

        var result = await Fdc3.RaiseIntent(request, SingleContext.Type);
        result.Should().NotBeNull();
        result.Response.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task RaiseIntent_calls_ResolverUI()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson()
        };

        var result = await Fdc3.RaiseIntent(request, MultipleContext.Type);
        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task RaiseIntent_returns_one_running_app()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await ModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Stored.Should().BeTrue();

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App4.AppId, InstanceId = targetFdc3InstanceId }
        };

        var result = await Fdc3.RaiseIntent(request, ContextTypes.Nothing);

        result.Should().NotBeNull();
        result.Response.AppMetadata.Should().NotBeNull();
        result.Response.AppMetadata!.AppId.Should().Be(App4.AppId);
        result.Response.AppMetadata!.InstanceId.Should().Be(targetFdc3InstanceId);
        result.RaiseIntentResolutionMessages.Should().NotBeEmpty();
        result.Response.Intent.Should().Be(IntentWithNoResult.Name);
        result.RaiseIntentResolutionMessages.Should().HaveCount(1);
        result.RaiseIntentResolutionMessages.First().TargetModuleInstanceId.Should().Be(targetFdc3InstanceId);
    }

    [Fact]
    public async Task RaiseIntent_logs_warning_when_the_raising_app_does_not_define_the_intent_in_the_AppDirectory_Raises_record()
    {
        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.GetFdc3InstanceId(origin);

        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = originFdc3InstanceId,
            Intent = IntentWithNoResult.Name,
            Context = CurrencyContext.AsJson(),
            TargetAppIdentifier = new AppIdentifier { AppId = App4.AppId }
        };

        // This should return an error anyway because no app can handle the intent
        _ = await Fdc3.RaiseIntent(request, ContextTypes.Currency);

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