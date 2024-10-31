using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData
{
    internal static class FindIntentAppDirectoryData
    {
        public static IntentMetadata SingleAppIntent = new IntentMetadata() { Name = "singleAppIntent", DisplayName = "Intent resolved by a single app" };
        public static IntentMetadata MultipleAppsIntent = new IntentMetadata { Name = "multipleAppsIntent", DisplayName = "Intent resolved by multiple apps" };
        public static IntentMetadata IntentWithNoResult = new IntentMetadata { Name = "intentWithNoResult", DisplayName = "Intent that has no result type" };

        public static Context SingleContext { get; } = new Context("singleContext");
        public static Context MultipleContext { get; } = new Context("multipleContext");

        public const string SingleResultType = "singleResultType";
        public const string MultipleResultType = "multipleResultType";

        public static AppMetadata App1 { get; } = new AppMetadata { AppId = "appId1", Name = "app1", ResultType = "singleResultType" };
        public static AppMetadata App2 { get; } = new AppMetadata { AppId = "appId2", Name = "app2", ResultType = "multipleResultType" };
        public static AppMetadata App3 { get; } = new AppMetadata { AppId = "appId3", Name = "app3", ResultType = "multipleResultType" };
        public static AppMetadata App4 { get; } = new AppMetadata { AppId = "appId4", Name = "app4", ResultType = "fdc3.nothing" };
        public static AppMetadata App5 { get; } = new AppMetadata { AppId = "appId5", Name = "app5", ResultType = null };
    }
}
