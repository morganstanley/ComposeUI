using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData;

internal static class FindIntentAppDirectoryData
{
    public static IntentMetadata Intent1 = new IntentMetadata() { Name = "intent1", DisplayName = "Intent resolved only by app 1" };
    public static IntentMetadata Intent2 = new IntentMetadata { Name = "intent2", DisplayName = "Intent resolved by apps 2 and 3" };
    public static IntentMetadata Intent3 = new IntentMetadata { Name = "intent3", DisplayName = "Intent resolved only by app 3" };
    public static IntentMetadata IntentWithNoResult = new IntentMetadata { Name = "intentWithNoResult", DisplayName = "Intent that has no result type" };

    public static Context SingleContext { get; } = new Context("singleContext");
    public static Context MultipleContext { get; } = new Context("multipleContext");

    public const string ResultType1 = "resultType1";
    public const string ResultType2 = "resultType2";

    public static AppMetadata App1 { get; } = new AppMetadata { AppId = "appId1", Name = "app1", ResultType = "resultType1" };
    public static AppMetadata App2 { get; } = new AppMetadata { AppId = "appId2", Name = "app2", ResultType = "resultType2" };
    public static AppMetadata App3ForIntent2 { get; } = new AppMetadata { AppId = "appId3", Name = "app3", ResultType = "resultType1" };
    public static AppMetadata App3ForIntent3 { get; } = new AppMetadata { AppId = "appId3", Name = "app3", ResultType = "resultType2" };
    public static AppMetadata App4 { get; } = new AppMetadata { AppId = "appId4", Name = "app4", ResultType = "fdc3.nothing" };
    public static AppMetadata App5 { get; } = new AppMetadata { AppId = "appId5", Name = "app5", ResultType = null };
}
