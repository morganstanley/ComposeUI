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
import { Fdc3ChannelMessageRouterMessage } from "./Fdc3ChannelMessageRouterMessage";

export class ComposeUIListener implements Listener {
    private messageRouterClient: MessageRouter;
    private unsubscribable?: Unsubscribable;
    private handler?: ContextHandler;
    private channelId?: string;
    private contextType?: string;
    private isSubscribed: boolean = true;

    constructor (messageRouterClient: MessageRouter, handler?: ContextHandler, channelId?: string, contextType?: string) {
        this.messageRouterClient = messageRouterClient;
        this.handler = handler;

        if(channelId != null) { //topic: "composeui/fdc3/v2.0/userchannels/{name}/"
            this.channelId = channelId;
        }
        
        this.contextType = contextType;
    }

    public async subscribe(channel: string): Promise<void> { //"composeui/fdc3/v2.0/userchannels/{name}/broadcast"
        if(this.handler === undefined || this.handler === null) {
            this.isSubscribed = false;
            throw new Error("No subscription have been established, due contextHandler have not been added.");
        } else {
            const subscribeTopic = this.channelId + channel;
            this.unsubscribable = await this.messageRouterClient.subscribe(subscribeTopic, (topicMessage: TopicMessage) => {
            //message payload format: {Context: {contextmessage}}
            //TODO: integration test to it
            const fdc3Message = new Fdc3ChannelMessageRouterMessage(topicMessage.topic, JSON.parse(topicMessage.payload!)); //Id: {{channelName}/{operation}} -> topicRoot
            if (this.channelId == fdc3Message.Id 
                && (this.contextType == null || this.contextType == fdc3Message.Context.type)) {
                    this.handler!(fdc3Message.Context);
            }
        });}
    } 

    public handleContextMessage(context: Context): Promise<void> {
        return new Promise((resolve, reject) => {
            if(!this.isSubscribed || this.handler === null || context === null || this.handler === undefined){
                reject(new Error("The current listener is not subscribed or the context/contextHandler hasn't been added."));
            } else {
                resolve(this.handler(context));
            }
        });
    }

    public unsubscribe(): Boolean {
        if (this.unsubscribable == null || this.unsubscribable == undefined || !this.isSubscribed) return false;
        this.unsubscribable.unsubscribe();
        this.isSubscribed = false;
        return true;
    }
}