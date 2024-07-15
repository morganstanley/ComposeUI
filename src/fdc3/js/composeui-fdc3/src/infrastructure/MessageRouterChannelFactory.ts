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

import { ChannelError, IntentHandler, Listener, PrivateChannel } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { Channel } from "@finos/fdc3";
import { ChannelFactory } from "./ChannelFactory";
import { Fdc3FindChannelRequest } from "./messages/Fdc3FindChannelRequest";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3FindChannelResponse } from "./messages/Fdc3FindChannelResponse";
import { ComposeUIChannel } from "./ComposeUIChannel";
import { ComposeUIIntentListener } from "./ComposeUIIntentListener";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { Fdc3IntentListenerResponse } from "./messages/Fdc3IntentListenerResponse";
import { Fdc3IntentListenerRequest } from "./messages/Fdc3IntentListenerRequest";

export class MessageRouterChannelFactory implements ChannelFactory {
    private messageRouterClient: MessageRouter;

    constructor(messageRouter: MessageRouter) {
        this.messageRouterClient = messageRouter;

    }

    public async GetUserChannel(channelId: string): Promise<Channel> {
        const topic = ComposeUITopic.joinUserChannel();
        const message = JSON.stringify(new Fdc3FindChannelRequest(channelId, "user"));
        const response = await this.messageRouterClient.invoke(topic, message);
        if (!response) {
            throw new Error(ChannelError.AccessDenied);
        }
        const fdc3Message = <Fdc3FindChannelResponse>JSON.parse(response);
        if (fdc3Message.error) {
            throw new Error(fdc3Message.error);
        }
        if (fdc3Message.found) {
            const channel = new ComposeUIChannel(channelId, "user", this.messageRouterClient);
            return channel;
        }
        else {
            throw new Error(ChannelError.NoChannelFound);
        }
    }

    public async GetIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        const listener = new ComposeUIIntentListener(this.messageRouterClient, intent, window.composeui.fdc3.config!.instanceId!, handler);
        await listener.registerIntentHandler();

        const message = new Fdc3IntentListenerRequest(intent, window.composeui.fdc3.config!.instanceId!, "Subscribe");
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
}