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
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class RaiseIntentForContextTests : Fdc3DesktopAgentTestsBase
{
    public RaiseIntentForContextTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task RaiseIntentForContext_returns_NoAppsFound()
    {
        var request = new RaiseIntentForContextRequest
        {
            Context = new Context("nosuchcontext").AsJson(),
        };
        var result = await Fdc3.RaiseIntentForContext(request, "nosuchcontext");

        result?.Response.Should().NotBeNull();
        result!.Response.Error.Should().Be(ResolveError.NoAppsFound);
    }


    [Fact]
    public async Task RaiseIntentForContext_with_multiple_possibilities_calls_ResolverUI()
    {
        ResolverUICommunicator
            .Setup(x => x.SendResolverUIIntentRequest(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolverUIIntentResponse()
            {
                SelectedIntent = Intent2.Name
            });

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = MultipleContext.AsJson()
        };

        var result = await Fdc3.RaiseIntentForContext(request, MultipleContext.Type);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIIntentRequest(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RaiseIntentForContext_with_single_intent_but_multiple_apps_calls_ResolverUI()
    {
        ResolverUICommunicator
        .Setup(x => x.SendResolverUIIntentRequest(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));


        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = ContextType.Nothing.AsJson(),
        };

        var result = await Fdc3.RaiseIntentForContext(request, ContextTypes.Nothing);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIRequest(It.IsAny<IEnumerable<IAppMetadata>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RaiseIntentForContext_with_multiple_intents_but_single_app_calls_ResolverUI()
    {
        ResolverUICommunicator
        .Setup(x => x.SendResolverUIIntentRequest(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ResolverUIIntentResponse()
        {
            SelectedIntent = IntentWithNoResult.Name
        });

        var request = new RaiseIntentForContextRequest
        {
            Fdc3InstanceId = Guid.NewGuid().ToString(),
            Context = OnlyApp3Context.AsJson()
        };

        var result = await Fdc3.RaiseIntentForContext(request, OnlyApp3Context.Type);

        ResolverUICommunicator.Verify(_ => _.SendResolverUIIntentRequest(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()));
        ResolverUICommunicator.VerifyNoOtherCalls();
    }
}
