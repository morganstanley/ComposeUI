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

import { Channel, Context, ContextHandler, DisplayMetadata, Listener } from "@finos/fdc3";
import { MessageBuffer, MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { ChannelType } from "./ChannelType";
import { ComposeUIListener } from "./ComposeUIListener";
import { Fdc3ChannelMessage } from "./messages/Fdc3ChannelMessage";
import { Fdc3GetCurrentContextMessage } from "./messages/Fdc3GetCurrentContextMessage";
import { ComposeUITopic } from "./ComposeUITopic";
import { randomUUID } from "crypto";

export class ComposeUIChannel implements Channel {
    id: string;
    type: "user" | "app" | "private";
    displayMetadata?: DisplayMetadata;

    private messageRouterClient!: MessageRouter;
    private lastContexts: Map<string, Context> = new Map<string, Context>();
    private lastContext?: Context;

    constructor(id: string, type: ChannelType, messageRouterClient: MessageRouter) {
        this.id = id;
        this.type = type;
        this.messageRouterClient = messageRouterClient;
    }

    public async broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;
        const message = new Fdc3ChannelMessage(this.id, context);
        const topic = ComposeUITopic.broadcast(this.id);
        await this.messageRouterClient.publish(topic, JSON.stringify(message));
    }

    //TODO add ChannelError
    public getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        return new Promise<Context | null>(async (resolve, reject) => {
            const message = JSON.stringify(new Fdc3GetCurrentContextMessage(contextType ?? null));
            await this.messageRouterClient.invoke(ComposeUITopic.getCurrentContext(this.id, this.type), message)
                .then((response) => {
                    if (response) {
                        const topicMessage = JSON.parse(response) as TopicMessage;
                        if(topicMessage.payload) {
                            const context = JSON.parse(topicMessage.payload) as Context;
                            if(context) {
                                this.lastContext = context;
                                this.lastContexts.set(context.type, context); //context type could be undefined?
                            }
                        }
                    }
                    resolve(this.retrieveCurrentContext(contextType))
                });
        });
    }

    public retrieveCurrentContext(contextType?: string | undefined): Context | null {
        let context: Context | undefined;
        if (contextType) {
            context = this.lastContexts.get(contextType);
            if (!context) {
                return null;
            }
        } else {
            context = this.lastContext;
        }

        return context ?? null;
    }

    public addContextListener(contextType: string | null, handler: ContextHandler): Promise<Listener>;
    public addContextListener(handler: ContextHandler): Promise<Listener>;
    public async addContextListener(contextType: any, handler?: any): Promise<Listener> {
        if (typeof contextType != 'string' && contextType != null) {
            throw new Error("addContextListener with contextType as ContextHandler is deprecated, please use the newer version.");
        } else {
            const listenerId = randomUUID();
            const listener = new ComposeUIListener(listenerId, this.messageRouterClient, handler, this.id, contextType);
            await listener.subscribe();
            return listener;
        };
    }
}