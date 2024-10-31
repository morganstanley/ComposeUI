using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using static MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData.FindIntentAppDirectoryData;
using Finos.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests
{
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
                Intent = SingleAppIntent.Name,
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
                Intent = SingleAppIntent.Name,
                ResultType = MultipleResultType
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
                Intent = SingleAppIntent.Name
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = SingleAppIntent,
                        Apps = new[] { App1 }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_single_app_with_context()
        {
            var request = new FindIntentRequest
            {
                Intent = SingleAppIntent.Name,
                Context = SingleContext
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should().BeEquivalentTo(new AppIntent()
            {
                Intent = SingleAppIntent,
                Apps = new[] { App1 }
            });
        }

        [Fact]
        public async Task FindIntent_returns_single_app_with_resultType()
        {
            var request = new FindIntentRequest
            {
                Intent = SingleAppIntent.Name,
                ResultType = SingleResultType
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should().BeEquivalentTo(
                new AppIntent()
                {
                    Intent = SingleAppIntent,
                    Apps = new[] { App1 }
                });
        }

        [Fact]
        public async Task FindIntent_returns_multiple_apps_for_intent()
        {
            var request = new FindIntentRequest
            {
                Intent = MultipleAppsIntent.Name
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = MultipleAppsIntent,
                        Apps = new[] { App2, App3 }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_multiple_apps_with_context()
        {
            var request = new FindIntentRequest
            {
                Intent = MultipleAppsIntent.Name,
                Context = MultipleContext
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = MultipleAppsIntent,
                        Apps = new[] { App2, App3 }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_multiple_apps_with_resultType()
        {
            var request = new FindIntentRequest
            {
                Intent = MultipleAppsIntent.Name,
                ResultType = MultipleResultType
            };

            var result = await Fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = MultipleAppsIntent,
                        Apps = new[] { App2, App3 }
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
}
