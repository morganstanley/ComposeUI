using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finos.Fdc3.Context;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;


using AppChannel = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels.AppChannel;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using DisplayMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.DisplayMetadata;
using IntentMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.IntentMetadata;
using Icon = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.Icon;
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.ImplementationMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests
{
    public partial class Fdc3DesktopAgentTests
    {
        [Fact]
        public async Task FindIntent_returns_NoAppsFound()
        {
            var request = new FindIntentRequest
            {
                Intent = "nosuchintent",
                Fdc3InstanceId = Guid.NewGuid().ToString()
            };

            var result = await _fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.Error.Should().Be(ResolveError.NoAppsFound);
        }

        [Fact]
        public async Task FindIntent_returns_single_app_for_intent()
        {
            var request = new FindIntentRequest
            {
                Intent = "singleAppIntent"
            };

            var result = await _fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = new IntentMetadata { Name = "singleAppIntent", DisplayName = "Intent resolved by a single app" },
                        Apps = new[]
                        {
                        new AppMetadata {AppId = "appId1", Name = "app1", ResultType = "singleResultType"}
                        }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_single_app_with_context()
        {
            var request = new FindIntentRequest
            {
                Intent = "singleAppIntent",
                Context = new Context("singleContext")
            };

            var result = await _fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = new IntentMetadata { Name = "singleAppIntent", DisplayName = "Intent resolved by a single app" },
                        Apps = new[]
                        {
                        new AppMetadata {AppId = "appId1", Name = "app1", ResultType = "singleResultType"}
                        }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_single_app_with_resultType()
        {
            var request = new FindIntentRequest
            {
                Intent = "singleAppIntent",
                ResultType = "singleResultType"
            };

            var result = await _fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = new IntentMetadata { Name = "singleAppIntent", DisplayName = "Intent resolved by a single app" },
                        Apps = new[]
                        {
                        new AppMetadata {AppId = "appId1", Name = "app1", ResultType = "singleResultType"}
                        }
                    });
        }

        [Fact]
        public async Task FindIntent_returns_multiple_apps_for_intent()
        {
            var request = new FindIntentRequest
            {
                Intent = "multipleAppsIntent"
            };

            var result = await _fdc3.FindIntent(request);
            result.Should().NotBeNull();
            result.AppIntent.Should()
                .BeEquivalentTo(
                    new AppIntent
                    {
                        Intent = new IntentMetadata { Name = "multipleAppsIntent", DisplayName = "Intent resolved by multiple apps" },
                        Apps = new[]
                        {
                        new AppMetadata {AppId = "appId2", Name = "app2", ResultType = null},
                        new AppMetadata {AppId = "appId3", Name = "app3", ResultType = null}
                        }
                    });
        }
    }
}
