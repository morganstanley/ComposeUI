/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 *  
 */

import { ChannelType } from "./ChannelType";

//TODO: Add more methods which returns the topics for ComposeUI's right channel
export class ComposeUITopic {
    private static readonly fdc3Version = "v2.0";
    private static readonly topicRoot = `ComposeUI/fdc3/${this.fdc3Version}`;
    private static readonly userChannels = 'userChannels';
    private static readonly appChannels = 'appChannels';
    private static readonly privateChannels = 'privateChannels';
    private static readonly broadcastSuffix = "broadcast";
    private static readonly getCurrentContextSuffix = "getCurrentContext";
    private static readonly createPrivateChannelSuffix = "createPrivateChannel";
    private static readonly raiseIntentSuffix = "raiseIntent";
    private static readonly addIntentListenerSuffix = "addIntentListener";
    private static readonly getIntentSuffix = "getIntentResult";
    private static readonly findIntentSuffix = "findIntent";
    private static readonly findIntentsByContextSuffix = "findIntentsByContext";
    private static readonly sendIntentResultSuffix = "sendIntentResult";
    private static readonly findChannelSuffix = "findChannel";
    private static readonly createAppChannelSuffix = "createAppChannel";
    private static readonly getUserChannelsSuffix = "getUserChannels";
    private static readonly joinUserChannelSuffix = "joinUserChannel";
    private static readonly joinPrivateChannelSuffix = "joinPrivateChannel";
    private static readonly getInfoSuffix = "getInfo";
    private static readonly findInstancesSuffix = "findInstances";
    private static readonly getAppMetadataSuffix = "getAppMetadata";
    private static readonly addContextListenerSuffix = "addContextListener";
    private static readonly removeContextListenerSuffix = "removeContextListener";
    private static readonly openSuffix = "open";
    private static readonly getOpenedAppContextSuffix = "getOpenedAppContext";
    private static readonly raiseIntentForContextSuffix = "raiseIntentForContext";

    public static broadcast(channelId: string, channelType: ChannelType = "user"): string {
        return `${this.getChannelsTopicRootWithChannelId(channelId, channelType)}/${this.broadcastSuffix}`;
    }

    public static getCurrentContext(channelId: string, channelType: ChannelType = "user"): string {
        return `${this.getChannelsTopicRootWithChannelId(channelId, channelType)}/${this.getCurrentContextSuffix}`;
    }

    public static createPrivateChannel(): string {
        return `${this.topicRoot}/${this.createPrivateChannelSuffix}`;
    }

    public static raiseIntent(intent?: string, instanceId?: string): string {
        if (intent && instanceId) {
            return `${this.topicRoot}/${this.raiseIntentSuffix}/${intent}/${instanceId}`;
        } else {
            return `${this.topicRoot}/${this.raiseIntentSuffix}`;
        }
    }

    public static addIntentListener(): string {
        return `${this.topicRoot}/${this.addIntentListenerSuffix}`;
    }

    public static getIntentResult(): string {
        return `${this.topicRoot}/${this.getIntentSuffix}`;
    }

    public static sendIntentResult(): string {
        return `${this.topicRoot}/${this.sendIntentResultSuffix}`;
    }

    public static findIntent(): string {
        return `${this.topicRoot}/${this.findIntentSuffix}`;
    }

    public static findIntentsByContext(): string {
        return `${this.topicRoot}/${this.findIntentsByContextSuffix}`;
    }

    public static findChannel(): string {
        return `${this.topicRoot}/${this.findChannelSuffix}`;
    }

    public static privateChannelInternalEvents(channelId: string) {
        return `${this.getChannelsTopicRootWithChannelId(channelId, "private")}/events`
    }

    public static privateChannelGetContextHandlers(channelId: string, isOriginalCreator: boolean) {
        return `${this.getChannelsTopicRootWithChannelId(channelId, "private")}/${isOriginalCreator ? "creator" : "listener"}/getContextHandlers`
    }

    public static createAppChannel(): string {
        return `${this.topicRoot}/${this.createAppChannelSuffix}`;
    }

    public static getUserChannels(): string {
        return `${this.topicRoot}/${this.getUserChannelsSuffix}`;
    }

    public static joinUserChannel(): string {
        return `${this.topicRoot}/${this.joinUserChannelSuffix}`;
    }

    public static getInfo(): string {
        return `${this.topicRoot}/${this.getInfoSuffix}`;
    }

    public static findInstances(): string {
        return `${this.topicRoot}/${this.findInstancesSuffix}`;
    }

    public static getAppMetadata(): string {
        return `${this.topicRoot}/${this.getAppMetadataSuffix}`;
    }

    public static addContextListener(): string {
        return `${this.topicRoot}/${this.addContextListenerSuffix}`;
    }

    public static removeContextListener(): string {
        return `${this.topicRoot}/${this.removeContextListenerSuffix}`;
    }

    private static getChannelsTopicRootWithChannelId(channelId: string, channelType: ChannelType): string {
        return `${this.getChannelsTopicRoot(channelType)}/${channelId}`;
    }

    public static open(): string {
        return `${this.topicRoot}/${this.openSuffix}`;
    }

    public static getOpenedAppContext(): string {
        return `${this.topicRoot}/${this.getOpenedAppContextSuffix}`;
    }

    public static raiseIntentForContext(): string {
        return `${this.topicRoot}/${this.raiseIntentForContextSuffix}`;
    }

    public static joinPrivateChannel(): string {
        return `${this.topicRoot}/${this.joinPrivateChannelSuffix}`
    }

    private static getChannelsTopicRoot(channelType: ChannelType): string {
        switch (channelType) {
            case "user":
                return `${this.topicRoot}/${this.userChannels}`;
            case "app":
                return `${this.topicRoot}/${this.appChannels}`;
            case "private":
                return `${this.topicRoot}/${this.privateChannels}`;
        }
    }
}