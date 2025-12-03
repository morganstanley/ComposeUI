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

public class FindIntentsByContextTests : Fdc3DesktopAgentServiceTestsBase
{
    public FindIntentsByContextTests() : base(AppDirectoryPath) { }

    [Fact]
    public async Task FindIntentsByContext_returns_NoAppsFound()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = new Context("nosuchcontext").AsJson()
        };
        var result = await Fdc3.FindIntentsByContext(request, "nosuchcontext");

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntentsByContext_returns_NoAppsFound_for_resultType()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = SingleContext.AsJson(),
            ResultType = "nosuchresulttype"
        };
        var result = await Fdc3.FindIntentsByContext(request, "nosuchresulttype");

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntentsByContext_returns_matching_apps()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = MultipleContext.AsJson()
        };
        var result = await Fdc3.FindIntentsByContext(request, MultipleContext.Type);

        result.Should().NotBeNull();
        result.AppIntents.Should().BeEquivalentTo(new[]
        {
            new AppIntent
            {
                Intent=Intent2,
                Apps=new[] { App2, App3ForIntent2 }
            },
            new AppIntent
            {
                Intent=Intent3,
                Apps=new[] { App3ForIntent3 }
            }
        });
    }

    [Fact]
    public async Task FindIntentsByContext_returns_matching_apps_for_resultType()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = MultipleContext.AsJson(),
            ResultType = ResultType1
        };
        var result = await Fdc3.FindIntentsByContext(request, MultipleContext.Type);

        result.Should().NotBeNull();
        result.AppIntents.Should().BeEquivalentTo(new[]
        {            
            new AppIntent
            {
                Intent=Intent2,
                Apps=new[] { App3ForIntent2 }
            }
        });
    }
}
