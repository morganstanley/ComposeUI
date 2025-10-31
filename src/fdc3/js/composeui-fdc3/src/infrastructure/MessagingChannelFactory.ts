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

import { ChannelError, ContextHandler, IntentHandler, Listener, PrivateChannel } from "@finos/fdc3";
import { JsonMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { Channel } from "@finos/fdc3";
import { ChannelFactory } from "./ChannelFactory";
import { ComposeUIPrivateChannel } from "./ComposeUIPrivateChannel";
import { Fdc3CreatePrivateChannelRequest } from "./messages/Fdc3CreatePrivateChannelRequest";
import { Fdc3CreatePrivateChannelResponse } from "./messages/Fdc3CreatePrivateChannelResponse";
import { Fdc3JoinPrivateChannelRequest } from "./messages/Fdc3JoinPrivateChannelRequest";
import { Fdc3JoinPrivateChannelResponse } from "./messages/Fdc3JoinPrivateChannelResponse";
import { Fdc3FindChannelRequest } from "./messages/Fdc3FindChannelRequest";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3FindChannelResponse } from "./messages/Fdc3FindChannelResponse";
import { ComposeUIChannel } from "./ComposeUIChannel";
import { ComposeUIIntentListener } from "./ComposeUIIntentListener";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { Fdc3IntentListenerResponse } from "./messages/Fdc3IntentListenerResponse";
import { Fdc3IntentListenerRequest } from "./messages/Fdc3IntentListenerRequest";
import { ChannelType } from "./ChannelType";
import { Fdc3CreateAppChannelRequest } from "./messages/Fdc3CreateAppChannelRequest";
import { Fdc3CreateAppChannelResponse } from "./messages/Fdc3CreateAppChannelResponse";
import { Fdc3GetUserChannelsRequest } from "./messages/Fdc3GetUserChannelsRequest";
import { Fdc3GetUserChannelsResponse } from "./messages/Fdc3GetUserChannelsResponse";
import { Fdc3JoinUserChannelRequest } from "./messages/Fdc3JoinUserChannelRequest";
import { Fdc3JoinUserChannelResponse } from "./messages/Fdc3JoinUserChannelResponse";
import { ChannelItem } from "./ChannelItem";
import { ComposeUIContextListener } from "./ComposeUIContextListener";

export class MessagingChannelFactory implements ChannelFactory {
    private jsonMessaging: JsonMessaging;
    private fdc3instanceId: string;

    constructor(jsonMessaging: JsonMessaging,fdc3instanceId: string) {
        this.jsonMessaging = jsonMessaging;
        this.fdc3instanceId = fdc3instanceId;
    }

    public async getChannel(channelId: string, channelType: ChannelType): Promise<Channel> {
        const topic = ComposeUITopic.findChannel();
        const message = new Fdc3FindChannelRequest(channelId, channelType);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3FindChannelRequest, Fdc3FindChannelResponse>(topic, message);
        if (!response) {
            throw new Error(ChannelError.AccessDenied);
        }

        if (response.error) {
            throw new Error(response.error);
        }
        if (!response.found) {
            throw new Error(ChannelError.NoChannelFound);
        }

        if (channelType == "private") {
            return await this.joinPrivateChannel(channelId);
        }
        return new ComposeUIChannel(channelId, channelType, this.jsonMessaging);
    }

    public async createPrivateChannel(): Promise<PrivateChannel> {
        // TODO: how to properly identify the other participant of the channel if the interface is parameterless?
        const message = new Fdc3CreatePrivateChannelRequest(this.fdc3instanceId);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3CreatePrivateChannelRequest, Fdc3CreatePrivateChannelResponse>(ComposeUITopic.createPrivateChannel(), message);

        if (response) {
            if (response.error) {
                throw new Error(response.error);
            }
            var channel = new ComposeUIPrivateChannel(response.channelId!, this.fdc3instanceId, this.jsonMessaging, true);
            return channel;
        }
        throw new Error(ChannelError.CreationFailed);
    }

    private async joinPrivateChannel(channelId: string): Promise<PrivateChannel> {
        const message = new Fdc3JoinPrivateChannelRequest(this.fdc3instanceId, channelId);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3JoinPrivateChannelRequest, Fdc3JoinPrivateChannelResponse>(ComposeUITopic.joinPrivateChannel(), message);
        if (!response) {
            throw new Error("No response received");
        }

        if (response.error) {
            throw new Error(response.error);
        }
        var channel = new ComposeUIPrivateChannel(channelId, this.fdc3instanceId, this.jsonMessaging, false);
        return channel;
    }

    public async createAppChannel(channelId: string): Promise<Channel> {
        var request = new Fdc3CreateAppChannelRequest(channelId, this.fdc3instanceId);
        var response = await this.jsonMessaging.invokeJsonService<Fdc3CreateAppChannelRequest, Fdc3CreateAppChannelResponse>(ComposeUITopic.createAppChannel(), request);
        if (!response) {
            throw new Error(ChannelError.CreationFailed);
        }


        if (response.error) {
            throw new Error(response.error);
        }

        if (!response.success) {
            throw new Error(ChannelError.CreationFailed);
        }

        return new ComposeUIChannel(channelId, "app", this.jsonMessaging);
    }

    public async joinUserChannel(channelId: string): Promise<Channel> {
        const topic: string = ComposeUITopic.joinUserChannel();
        const request: Fdc3JoinUserChannelRequest = new Fdc3JoinUserChannelRequest(channelId, this.fdc3instanceId);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3JoinUserChannelRequest, Fdc3JoinUserChannelResponse>(topic, request);

        if (!response) {
            throw new Error(ChannelError.CreationFailed);
        }

        if (response.error) {
            throw new Error(response.error);
        }

        if (!response.success) {
            throw new Error(ChannelError.CreationFailed);
        }

        var channel = new ComposeUIChannel(channelId, "user", this.jsonMessaging, response.displayMetadata);
        return channel;
    }

    public async getUserChannels(): Promise<Channel[]> {
        var request: Fdc3GetUserChannelsRequest = new Fdc3GetUserChannelsRequest(this.fdc3instanceId);

        var response = await this.jsonMessaging.invokeJsonService<Fdc3GetUserChannelsRequest, Fdc3GetUserChannelsResponse>(ComposeUITopic.getUserChannels(), request);
        if (!response) {
            throw new Error(ChannelError.NoChannelFound);
        }

        if (response.error) {
            throw new Error(response.error);
        }

        var channels: Channel[] = [];
        response.channels!.forEach((channelItem: ChannelItem) => {
            var channel = new ComposeUIChannel(channelItem.id, "user", this.jsonMessaging, channelItem.displayMetadata);
            channels.push(channel);
        });

        return channels;
    }

    public async getIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        const listener = new ComposeUIIntentListener(this.jsonMessaging, intent, this.fdc3instanceId, handler);
        await listener.registerIntentHandler();

        const message = new Fdc3IntentListenerRequest(intent, this.fdc3instanceId, "Subscribe");
        const response = await this.jsonMessaging.invokeJsonService<Fdc3IntentListenerRequest, Fdc3IntentListenerResponse>(ComposeUITopic.addIntentListener(), message);
        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        if (response.error) {
            await listener.unsubscribe();
            throw new Error(response.error);
        } else if (!response.stored) {
            await listener.unsubscribe();
            throw new Error(ComposeUIErrors.SubscribeFailure);
        }

        return listener;
    }

    public async getContextListener(openHandled: boolean, channel?: Channel, handler?: ContextHandler, contextType?: string | null): Promise<Listener> {
        if (channel) {

            if (channel instanceof ComposeUIChannel) {
                (<ComposeUIChannel>channel).setOpenHandled(openHandled);
            }

            const listener = <ComposeUIContextListener>await channel.addContextListener(contextType ?? null, handler!);
            return listener;
        }

        const listener = new ComposeUIContextListener(openHandled, this.jsonMessaging, handler!, contextType ?? undefined);
        return listener;
    }
}
