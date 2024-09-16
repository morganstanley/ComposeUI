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
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { Channel } from "@finos/fdc3";
import { ChannelFactory } from "./ChannelFactory";
import { ComposeUIPrivateChannel } from "./ComposeUIPrivateChannel";
import { Fdc3CreatePrivateChannelRequest } from "./messages/Fdc3CreatePrivateChannelRequest";
import { Fdc3CreatePrivateChannelResponse } from "./messages/Fdc3CreatePrivateChannelResponse";
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

export class MessageRouterChannelFactory implements ChannelFactory {
    private messageRouterClient: MessageRouter;
    private fdc3instanceId: string;

    constructor(messageRouter: MessageRouter, fdc3instanceId: string) {
        this.messageRouterClient = messageRouter;
        this.fdc3instanceId = fdc3instanceId;
    }

    public async getChannel(channelId: string, channelType: ChannelType): Promise<Channel> {
        const topic = ComposeUITopic.findChannel();
        const message = JSON.stringify(new Fdc3FindChannelRequest(channelId, channelType));
        const response = await this.messageRouterClient.invoke(topic, message);
        if (!response) {
            throw new Error(ChannelError.AccessDenied);
        }
        const fdc3Message = <Fdc3FindChannelResponse>JSON.parse(response);
        if (fdc3Message.error) {
            throw new Error(fdc3Message.error);
        }
        if (!fdc3Message.found) {
            throw new Error(ChannelError.NoChannelFound);
        }

        if (channelType == "private") {
            return new ComposeUIPrivateChannel(channelId, this.messageRouterClient, false);
        }
        return new ComposeUIChannel(channelId, channelType, this.messageRouterClient);
    }

    public async createPrivateChannel(): Promise<PrivateChannel> {
        // TODO: how to properly identify the other participant of the channel if the interface is parameterless?
        const message = JSON.stringify(new Fdc3CreatePrivateChannelRequest());
        const response = await this.messageRouterClient.invoke(ComposeUITopic.createPrivateChannel(), message);
        if (response) {
            const fdc3response = <Fdc3CreatePrivateChannelResponse>JSON.parse(response);
            if (fdc3response.error) {
                throw new Error(fdc3response.error);
            }
            var channel = new ComposeUIPrivateChannel(fdc3response.channelId!, this.messageRouterClient, true);
            return channel;
        }
        throw new Error(ChannelError.CreationFailed);
    }

    public async createAppChannel(channelId: string): Promise<Channel> {
        var request = JSON.stringify(new Fdc3CreateAppChannelRequest(channelId, this.fdc3instanceId));
        var result = await this.messageRouterClient.invoke(ComposeUITopic.createAppChannel(), request);
        if (!result) {
            throw new Error(ChannelError.CreationFailed);
        }

        const response = <Fdc3CreateAppChannelResponse>JSON.parse(result);
        if (response.error) {
            throw new Error(response.error);
        }

        if (!response.success) {
            throw new Error(ChannelError.CreationFailed);
        }

        return new ComposeUIChannel(channelId, "app", this.messageRouterClient);
    }

    public async joinUserChannel(channelId: string): Promise<Channel> {
        const topic: string = ComposeUITopic.joinUserChannel();
        const request: string = JSON.stringify(new Fdc3JoinUserChannelRequest(channelId, this.fdc3instanceId));
        const response = await this.messageRouterClient.invoke(topic, request);

        if (!response) {
            throw new Error(ChannelError.CreationFailed);
        }

        const message = <Fdc3JoinUserChannelResponse>JSON.parse(response);
        if (message.error) {
            throw new Error(message.error);
        }

        if (!message.success) {
            throw new Error(ChannelError.CreationFailed);
        }

        var channel = new ComposeUIChannel(channelId, "user", this.messageRouterClient, message.displayMetadata);
        return channel;
    }

    public async getUserChannels(): Promise<Channel[]> {
        var request: string = JSON.stringify(new Fdc3GetUserChannelsRequest(this.fdc3instanceId));

        var result = await this.messageRouterClient.invoke(ComposeUITopic.getUserChannels(), request);
        if (!result) {
            throw new Error(ChannelError.NoChannelFound);
        }

        const response = <Fdc3GetUserChannelsResponse>JSON.parse(result);
        if (response.error) {
            throw new Error(response.error);
        }

        var channels: Channel[] = [];
        response.channels!.forEach((channelItem: ChannelItem) => {
            var channel = new ComposeUIChannel(channelItem.id, "user", this.messageRouterClient, channelItem.displayMetadata);
            channels.push(channel);
        });

        return channels;
    }

    public async getIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        const listener = new ComposeUIIntentListener(this.messageRouterClient, intent, this.fdc3instanceId, handler);
        await listener.registerIntentHandler();

        const message = new Fdc3IntentListenerRequest(intent, this.fdc3instanceId, "Subscribe");
        const response = await this.messageRouterClient.invoke(ComposeUITopic.addIntentListener(), JSON.stringify(message));
        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }
        const result = <Fdc3IntentListenerResponse>JSON.parse(response);
        if (result.error) {
            await listener.unsubscribe();
            throw new Error(result.error);
        } else if (!result.stored) {
            await listener.unsubscribe();
            throw new Error(ComposeUIErrors.SubscribeFailure);
        }

        return listener;
    }

    public async getContextListener(channel?: Channel, handler?: ContextHandler, contextType?: string | null): Promise<Listener> {
        if (channel) {
            const listener = <ComposeUIContextListener>await channel.addContextListener(contextType ?? null, handler!);
            return listener;
        }

        const listener = new ComposeUIContextListener(this.messageRouterClient, handler!, contextType ?? undefined);
        return listener;
    }
}