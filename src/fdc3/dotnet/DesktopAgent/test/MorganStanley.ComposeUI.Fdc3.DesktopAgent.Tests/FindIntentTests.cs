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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.TestAppDirectoryData;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIntent;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class FindIntentTests : Fdc3DesktopAgentTestsBase
{
    public FindIntentTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound()
    {
        var request = new FindIntentRequest
        {
            Intent = "nosuchintent"
        };

        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound_for_context()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name,
            Context = MultipleContext.AsJson()
        };
        var result = await Fdc3.FindIntent(request, MultipleContext.Type);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound_for_resultType()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name,
            ResultType = ResultType2
        };
        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntent_returns_single_app_for_intent()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name
        };

        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.AppIntent.Should()
            .BeEquivalentTo(
                new AppIntent
                {
                    Intent = Intent1,
                    Apps = new[] { App1 }
                });
    }

    [Fact]
    public async Task FindIntent_returns_single_app_with_context()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name,
            Context = SingleContext.AsJson()
        };

        var result = await Fdc3.FindIntent(request, SingleContext.Type);
        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(new AppIntent()
        {
            Intent = Intent1,
            Apps = new[] { App1 }
        });
    }

    [Fact]
    public async Task FindIntent_returns_single_app_with_resultType()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name,
            ResultType = ResultType1
        };

        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(
            new AppIntent()
            {
                Intent = Intent1,
                Apps = new[] { App1 }
            });
    }

    [Fact]
    public async Task FindIntent_returns_multiple_apps_for_intent()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent2.Name
        };

        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.AppIntent.Should()
            .BeEquivalentTo(
                new AppIntent
                {
                    Intent = Intent2,
                    Apps = new[] { App2, App3ForIntent2 }
                });
    }

    [Fact]
    public async Task FindIntent_returns_multiple_apps_with_context()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent2.Name,
            Context = MultipleContext.AsJson()
        };

        var result = await Fdc3.FindIntent(request, MultipleContext.Type);
        result.Should().NotBeNull();
        result.AppIntent.Should()
            .BeEquivalentTo(
                new AppIntent
                {
                    Intent = Intent2,
                    Apps = new[] { App2, App3ForIntent2 }
                });
    }

    [Fact]
    public async Task FindIntent_returns_multiple_apps_with_resultType()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent2.Name,
            ResultType = ResultType2
        };

        var result = await Fdc3.FindIntent(request, null);
        result.Should().NotBeNull();
        result.AppIntent.Should()
            .BeEquivalentTo(
                new AppIntent
                {
                    Intent = Intent2,
                    Apps = new[] { App2 }
                });
    }

    [Fact]
    public async Task FindIntent_returns_apps_with_no_result()
    {
        var request = new FindIntentRequest
        {
            Intent = IntentWithNoResult.Name,
            ResultType = ContextTypes.Nothing
        };
        var result = await Fdc3.FindIntent(request, ContextTypes.Nothing);

        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(
            new AppIntent
            {
                Intent = IntentWithNoResult,
                Apps = new[] { App4, App5 }
            });
    }

    // According to the current state of discussion in https://github.com/finos/FDC3/issues/1410 querying for fdc3.nothing should only match intents that have this explicitly stated.
    // I asked for confirmation as this leads to anomalies in case of an empty contexts array
    [Fact]
    public async Task FindIntent_returns_apps_with_nothing_context()
    {
        var request = new FindIntentRequest
        {
            Intent = IntentWithNoResult.Name,
            Context = ContextType.Nothing.AsJson()
        };
        var result = await Fdc3.FindIntent(request, ContextTypes.Nothing);

        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(
            new AppIntent
            {
                Intent = IntentWithNoResult,
                Apps = new[] { App4, App5 }
            });
    }
}
