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

using System.Text.Json;
using System.Text.Json.Serialization;
using Finos.Fdc3.Context;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.TestData;

internal static class TestAppDirectoryData
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new AppMetadataJsonConverter(),
            new IntentMetadataJsonConverter(),
            new AppIntentJsonConverter(),
            new DisplayMetadataJsonConverter(),
            new IconJsonConverter(),
            new ImageJsonConverter(),
            new IntentMetadataJsonConverter(),
            new ImplementationMetadataJsonConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static string AppDirectoryPath = @$"file:\\{Directory.GetCurrentDirectory()}\TestData\testAppDirectory.json";

    public static IntentMetadata Intent1 = new IntentMetadata() { Name = "intent1", DisplayName = "Intent resolved only by app 1" };
    public static IntentMetadata Intent2 = new IntentMetadata { Name = "intent2", DisplayName = "Intent resolved by apps 2 and 3" };
    public static IntentMetadata Intent3 = new IntentMetadata { Name = "intent3", DisplayName = "Intent resolved only by app 3" };
    public static IntentMetadata IntentWithNoResult = new IntentMetadata { Name = "intentWithNoResult", DisplayName = "Intent that has no result type" };
    public static IntentMetadata IntentWithChannelResult = new IntentMetadata { Name = "intentWithChannelResult", DisplayName = "Intent that returns a channel" };

    public static Context SingleContext { get; } = new Context("singleContext");
    public static Context MultipleContext { get; } = new Context("multipleContext");
    public static Context Intent2Context { get; } = new Context("intent2Context");
    public static Context OnlyApp3Context { get; } = new Context("onlyApp3Context");
    public static Context ChannelContext { get; } = new Context("channelContext");
    public static Context GenericChannelContext { get; } = new Context("genericChannelContext");
    public static Context SpecificChannelContext { get; } = new Context("specificChannelContext");
    public static Context CurrencyContext { get; } = new Context(ContextTypes.Currency);

    internal static string AsJson(this Context ctx)
    {
        return JsonSerializer.Serialize(ctx, JsonSerializerOptions);
    }

    public const string ResultType1 = "resultType1";
    public const string ResultType2 = "resultType2";
    public const string ChannelResult = "channel";
    public const string SpecificChannelResult = "channel<ctx>";

    public static AppMetadata App1 => new AppMetadata { AppId = "appId1", Name = "app1", ResultType = ResultType1 };
    public static AppMetadata App2 => new AppMetadata { AppId = "appId2", Name = "app2", ResultType = ResultType2 };
    public static AppMetadata App3ForIntent2 => new AppMetadata { AppId = "appId3", Name = "app3", ResultType = ResultType1 };
    public static AppMetadata App3ForIntent3 => new AppMetadata { AppId = "appId3", Name = "app3", ResultType = ResultType2 };
    public static AppMetadata App4 => new AppMetadata { AppId = "appId4", Name = "app4", ResultType = ContextTypes.Nothing };
    public static AppMetadata App5 => new AppMetadata { AppId = "appId5", Name = "app5", ResultType = null };
    public static AppMetadata App6 => new AppMetadata { AppId = "appId6", Name = "app6", ResultType = ChannelResult };
    public static AppMetadata App7 => new AppMetadata { AppId = "appId7", Name = "app7", ResultType = SpecificChannelResult };

}
