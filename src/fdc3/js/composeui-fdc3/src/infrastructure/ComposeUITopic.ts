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
    private static readonly joinUserChannelSuffix = "joinUserChannel";
    private static readonly getOrCreateChannelSuffix = "getOrCreateChannel";

    public static broadcast(channelId: string, channelType: ChannelType = "user") {
        return `${this.getChannelsTopicRootWithTopicId(channelId, channelType)}/${this.broadcastSuffix}`;
    }

    public static getCurrentContext(channelId: string, channelType: ChannelType = "user") {
        return `${this.getChannelsTopicRootWithTopicId(channelId, channelType)}/${this.getCurrentContextSuffix}`;
    }

    public static joinUserChannel() {
        return `${this.topicRoot}/${this.joinUserChannelSuffix}`;
    }

    public static getOrCreateChannel() {
        return `${this.topicRoot}/${this.getOrCreateChannelSuffix}`;
    }

    private static getChannelsTopicRootWithTopicId(topicId: string, channelType: ChannelType = "user") {
        return `${this.getChannelsTopicRoot(channelType)}/${topicId}`;
    }

    private static getChannelsTopicRoot(channelType: ChannelType = "user") {
        switch(channelType) {
            case "user":
                return `${this.topicRoot}/${this.userChannels}`;
            case "app":
                return `${this.topicRoot}/${this.appChannels}`;
            case "private":
                return `${this.topicRoot}/${this.privateChannels}`;
        }
    }
}