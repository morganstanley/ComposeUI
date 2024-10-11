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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Helpers;
using MorganStanley.ComposeUI.ModuleLoader;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class RaiseIntentTests : Fdc3DesktopAgentTestsBase
{
    public RaiseIntentTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task RaiseIntent_returns_NoAppsFound()
    {
        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
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
        var request = new RaiseIntentRequest
        {
            MessageId = 1,
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson()
        };

        var result = await Fdc3.RaiseIntent(request, MultipleContext.Type);
        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task RaiseIntent_returns_one_running_app()
    {
        await Fdc3.StartAsync(CancellationToken.None);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var origin = await ModuleLoader.Object.StartModule(new StartRequest(App1.AppId));
        var originFdc3InstanceId = Fdc3InstanceIdRetriever.Get(origin);

        //TODO: should add some identifier to the query => "fdc3:" + instance.Manifest.Id
        var target = await ModuleLoader.Object.StartModule(new StartRequest(App4.AppId));
        var targetFdc3InstanceId = Fdc3InstanceIdRetriever.Get(target);

        var addIntentListenerRequest = new IntentListenerRequest
        {
            Intent = IntentWithNoResult.Name,
            Fdc3InstanceId = targetFdc3InstanceId,
            State = SubscribeState.Subscribe
        };

        var addIntentListenerResponse = await Fdc3.AddIntentListener(addIntentListenerRequest);
        addIntentListenerResponse.Should().NotBeNull();
        addIntentListenerResponse.Response.Stored.Should().BeTrue();
        addIntentListenerResponse.RaiseIntentResolutionMessages.Should().BeEmpty();

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
}