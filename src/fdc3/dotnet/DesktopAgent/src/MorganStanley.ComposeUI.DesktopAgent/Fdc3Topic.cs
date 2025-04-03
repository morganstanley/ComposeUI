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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal static class Fdc3Topic
{
    internal static string TopicRoot => "ComposeUI/fdc3/v2.0/";
    internal static string FindChannel => TopicRoot + "findChannel";
    internal static string FindIntent => TopicRoot + "findIntent";
    internal static string FindIntentsByContext => TopicRoot + "findIntentsByContext";
    internal static string RaiseIntent => TopicRoot + "raiseIntent";
    internal static string GetIntentResult => TopicRoot + "getIntentResult";
    internal static string SendIntentResult => TopicRoot + "sendIntentResult";
    internal static string AddIntentListener => TopicRoot + "addIntentListener";
    internal static string ResolverUI => TopicRoot + "resolverUI";
    internal static string CreatePrivateChannel => TopicRoot + "createPrivateChannel";
    internal static string CreateAppChannel => TopicRoot + "createAppChannel";
    internal static string GetUserChannels => TopicRoot + "getUserChannels";
    internal static string JoinUserChannel => TopicRoot + "joinUserChannel";
    internal static string GetInfo => TopicRoot + "getInfo";
    internal static string FindInstances => TopicRoot + "findInstances";
    internal static string GetAppMetadata => TopicRoot + "getAppMetadata";
    internal static string AddContextListener => TopicRoot + "addContextListener";
    internal static string RemoveContextListener => TopicRoot + "removeContextListener";
    internal static string Open => TopicRoot + "open";
    internal static string GetOpenedAppContext => TopicRoot + "getOpenedAppContext";
    internal static string RaiseIntentForContext => TopicRoot + "raiseIntentForContext";
    internal static string ResolverUIIntent => TopicRoot + "resolverUIIntent";

    //IntentListeners will be listening at this endpoint
    internal static string RaiseIntentResolution(string intent, string instanceId)
    {
        return $"{RaiseIntent}/{intent}/{instanceId}";
    }

    internal static ChannelTopics UserChannel(string id) => new ChannelTopics(id, ChannelType.User);
    internal static ChannelTopics AppChannel(string id) => new ChannelTopics(id, ChannelType.App);
    internal static PrivateChannelTopics PrivateChannel(string id) => new PrivateChannelTopics(id);
}

internal class ChannelTopics
{
    protected readonly string ChannelRoot;
    internal ChannelTopics(string id, ChannelType type)
    {
        var channelTypeString = type switch
        {
            ChannelType.User => "userChannels",
            ChannelType.Private => "privateChannels",
            ChannelType.App => "appChannels",
            _ => throw new NotSupportedException($"{nameof(type)}")
        };

        ChannelRoot = $"{Fdc3Topic.TopicRoot}{channelTypeString}/{id}/";
        Broadcast = ChannelRoot + "broadcast";
        GetCurrentContext = ChannelRoot + "getCurrentContext";
    }

    public string Broadcast { get; }
    public string GetCurrentContext { get; }
}

internal class PrivateChannelTopics : ChannelTopics
{
    internal PrivateChannelTopics(string id) : base(id, ChannelType.Private)
    {
        Events = ChannelRoot + "events";
    }

    public string Events { get; }
}