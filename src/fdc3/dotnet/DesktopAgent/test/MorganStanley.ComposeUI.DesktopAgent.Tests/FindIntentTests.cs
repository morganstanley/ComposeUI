using Finos.Fdc3;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.FindIntentAppDirectoryData;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class FindIntentTests : Fdc3DesktopAgentTestsBase
{
    public FindIntentTests() : base(@$"file:\\{Directory.GetCurrentDirectory()}\TestData\findIntentAppDirectory.json") { }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound()
    {
        var request = new FindIntentRequest
        {
            Intent = "nosuchintent"
        };

        var result = await Fdc3.FindIntent(request);
        result.Should().NotBeNull();
        result.Error.Should().Be(ResolveError.NoAppsFound);
    }

    [Fact]
    public async Task FindIntent_returns_NoAppsFound_for_context()
    {
        var request = new FindIntentRequest
        {
            Intent = Intent1.Name,
            Context = MultipleContext
        };
        var result = await Fdc3.FindIntent(request);
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
        var result = await Fdc3.FindIntent(request);
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

        var result = await Fdc3.FindIntent(request);
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
            Context = SingleContext
        };

        var result = await Fdc3.FindIntent(request);
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

        var result = await Fdc3.FindIntent(request);
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

        var result = await Fdc3.FindIntent(request);
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
            Context = MultipleContext
        };

        var result = await Fdc3.FindIntent(request);
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

        var result = await Fdc3.FindIntent(request);
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
            ResultType = "fdc3.nothing"
        };
        var result = await Fdc3.FindIntent(request);

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
            Context = ContextType.Nothing
        };
        var result = await Fdc3.FindIntent(request);

        result.Should().NotBeNull();
        result.AppIntent.Should().BeEquivalentTo(
            new AppIntent
            {
                Intent = IntentWithNoResult,
                Apps = new[] { App4, App5 }
            });
    }
}
