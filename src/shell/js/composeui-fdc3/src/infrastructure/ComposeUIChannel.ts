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
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { ChannelType } from "./ChannelType";
import { ComposeUIListener } from "./ComposeUIListener";
import { Fdc3ChannelMessage } from "./messages/Fdc3ChannelMessage";
import { ComposeUITopic } from "./ComposeUITopic";

export class ComposeUIChannel implements Channel{
    id: string;
    type: "user" | "app" | "private";
    displayMetadata?: DisplayMetadata;

    private messageRouterClient!: MessageRouter;
    private lastContexts: Map<string, Context> = new Map<string, Context>();
    private lastContext?: Context;

    constructor(id: string, type: ChannelType, messageRouterClient: MessageRouter){
        this.id = id; 
        this.type = type;
        this.messageRouterClient = messageRouterClient;
    }
    
    //TODO: broadcast on both appchannels and userchannels they are subscribed.
    public async broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;

        //TODO: more topic message will be created
        const message = JSON.stringify(new Fdc3ChannelMessage(this.id, context));
        await this.messageRouterClient.publish(ComposeUITopic.broadcast(this.id), message);
    }

    public getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        return new Promise<Context>(async (resolve, reject) => {
            let context: Context | undefined;
            if (contextType) {
                context = this.lastContexts.get(contextType);
                if (!context){
                    reject(new Error(`The given contextType: ${contextType} was not found in the saved contexts.`));
                }
            } else {
                context = this.lastContext;
            }
            resolve(context!);
        });
    }

    public addContextListener(contextType: string | null, handler: ContextHandler): Promise<Listener>;
    public addContextListener(handler: ContextHandler): Promise<Listener>;
    public async addContextListener(contextType: any, handler?: any): Promise<Listener> {
        if(typeof contextType != 'string' && contextType != null){
            throw new Error("addContextListener with contextType as ContextHandler is deprecated, please use the newer version.");
        } else {
            const listener = new ComposeUIListener(this.messageRouterClient, handler, this.id, contextType);
            await listener.subscribe();

            await this.getCurrentContext(contextType)
                .then(async (resultContext) => {
                    listener.LatestContext = await this.getCurrentContext(contextType);
                    if(resultContext != listener.LatestContext) {
                        //TODO: test
                        await listener.handleContextMessage();
                    } else {
                        await listener.handleContextMessage(resultContext);
                    }
                }); //TODO: what happens whe a broadcasted message arrives between 2 points,

            return listener;
        };
    }
}