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

import { Context, ContextHandler, Listener } from "@finos/fdc3";
import { MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { Unsubscribable } from "rxjs";
import { Fdc3ChannelMessage } from "./Fdc3ChannelMessage";
import { ComposeUITopic } from "./ComposeUITopic";

export class ComposeUIListener implements Listener {
    private messageRouterClient: MessageRouter;
    private unsubscribable?: Unsubscribable;
    private handler: ContextHandler;
    private channelId?: string;
    private contextType?: string;
    private isSubscribed: boolean = false;

    constructor(messageRouterClient: MessageRouter, handler: ContextHandler, channelId?: string, contextType?: string) {
        this.messageRouterClient = messageRouterClient;
        this.handler = handler;

        if (channelId) { 
            this.channelId = channelId;
        }

        this.contextType = contextType;
    }

    public async subscribe(): Promise<void> { 
        const subscribeTopic = ComposeUITopic.broadcast(this.channelId!);
        this.unsubscribable = await this.messageRouterClient.subscribe(subscribeTopic, (topicMessage: TopicMessage) => {
            //TODO: integration test to it
            const fdc3Message = new Fdc3ChannelMessage(topicMessage.topic, JSON.parse(topicMessage.payload!)); //Id: {{channelName}/{operation}} -> topicRoot
            if (this.channelId == fdc3Message.Id
                && (this.contextType == null || this.contextType == fdc3Message.Context.type)) {
                this.handler!(fdc3Message.Context);
            }
        });
        this.isSubscribed = true;
    }

    public handleContextMessage(context: Context): Promise<void> {
        return new Promise((resolve, reject) => {
            if (!this.isSubscribed ) {
                reject(new Error("The current listener is not subscribed."));
            } else {
                resolve(this.handler(context));
            }
        });
    }

    public unsubscribe(): Boolean {
        if (!this.unsubscribable || !this.isSubscribed) return false;
        this.unsubscribable.unsubscribe();
        this.isSubscribed = false;
        return true;
    }
}