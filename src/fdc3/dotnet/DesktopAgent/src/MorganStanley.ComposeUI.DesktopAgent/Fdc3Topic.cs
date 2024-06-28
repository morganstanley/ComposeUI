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

using System.Threading.Tasks.Dataflow;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

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

    //IntentListeners will be listening at this endpoint
    internal static string RaiseIntentResolution(string intent, string instanceId)
    {
        return $"{RaiseIntent}/{intent}/{instanceId}";
    }

    internal static ChannelTopics UserChannel(string id) => new ChannelTopics(id, ChannelType.User);

    internal static ChannelTopics PrivateChannel(string id) => new ChannelTopics(id, ChannelType.Private);
}

internal class ChannelTopics
{
    private readonly string _channelRoot;
    internal ChannelTopics(string id, ChannelType type)
    {
        string channelTypeString;
        switch (type)
        {
            case ChannelType.User:
                channelTypeString = "userChannels";
                break;
            case ChannelType.Private:
                channelTypeString = "privateChannels";
                break;
            default:
                throw new NotImplementedException("Channel type not implemented yet");
        }

        _channelRoot = $"{Fdc3Topic.TopicRoot}{channelTypeString}/{id}/";
        Broadcast = _channelRoot + "broadcast";
        GetCurrentContext = _channelRoot + "getCurrentContext";
    }

    public string Broadcast { get; }
    public string GetCurrentContext { get; }
}