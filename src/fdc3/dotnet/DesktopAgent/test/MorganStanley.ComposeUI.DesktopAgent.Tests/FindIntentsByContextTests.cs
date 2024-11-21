using Finos.Fdc3;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.FindIntentAppDirectoryData;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class FindIntentsByContextTests : Fdc3DesktopAgentTestsBase
{
    public FindIntentsByContextTests() : base(@$"file:\\{Directory.GetCurrentDirectory()}\TestData\findIntentAppDirectory.json") { }

    [Fact]
    public async Task FindIntentsByContext_returns_NoAppsFound()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = new Context("nosuchcontext")
        };
        var result = await Fdc3.FindIntentsByContext(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntentsByContext_returns_NoAppsFound_for_resultType()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = SingleContext,
            ResultType = "nosuchresulttype"
        };
        var result = await Fdc3.FindIntentsByContext(request);

        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntentsByContext_returns_matching_apps()
    {
        var request = new FindIntentsByContextRequest
        {
            Context = MultipleContext
        };
        var result = await Fdc3.FindIntentsByContext(request);

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
            Context = MultipleContext,
            ResultType = ResultType1
        };
        var result = await Fdc3.FindIntentsByContext(request);

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
