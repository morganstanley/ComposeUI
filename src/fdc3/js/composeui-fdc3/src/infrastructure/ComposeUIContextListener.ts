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
import { ChannelType } from "./ChannelType";
import { Unsubscribable } from "rxjs";
import { ComposeUITopic } from "./ComposeUITopic";

export class ComposeUIContextListener implements Listener {
    private messageRouterClient: MessageRouter;
    private unsubscribable?: Unsubscribable;
    private handler: ContextHandler;
    private channelId: string;
    private channelType: ChannelType;
    private contextType?: string;
    private isSubscribed: boolean = false;
    public latestContext: Context | null = null;

    constructor(messageRouterClient: MessageRouter, handler: ContextHandler, channelId: string, channelType: ChannelType, contextType?: string) {
        this.messageRouterClient = messageRouterClient;
        this.handler = handler;
        this.channelId = channelId;
        this.channelType = channelType;
        this.contextType = contextType;
    }

    public async subscribe(): Promise<void> { 
        const subscribeTopic = ComposeUITopic.broadcast(this.channelId, this.channelType);
        this.unsubscribable = await this.messageRouterClient.subscribe(subscribeTopic, (topicMessage: TopicMessage) => {
            if(topicMessage.context.sourceId == this.messageRouterClient.clientId) return;
            //TODO: integration test
            const context = <Context>JSON.parse(topicMessage.payload!);            
            if(!this.contextType || this.contextType == context!.type) {                
                this.handler!(context!);
            }
        });
        this.isSubscribed = true;
    }

    public handleContextMessage(context: Context | null = null): Promise<void> {
        return new Promise((resolve, reject) => {
            if (!this.isSubscribed ) {
                reject(new Error("The current listener is not subscribed."));
            } else {
                if(context) {
                    resolve(this.handler(context));
                } else {
                    if (this.latestContext) {
                        resolve(this.handler(this.latestContext));
                    } else {
                        resolve(this.handler({type: ""}));
                    }
                }            
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