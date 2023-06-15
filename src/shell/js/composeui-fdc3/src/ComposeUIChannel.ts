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
import { Fdc3ChannelMessage } from "./Fdc3ChannelMessage";
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

    public broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;
        const fdc3Message = new Fdc3ChannelMessage(this.id, context);
        return this.messageRouterClient.publish(ComposeUITopic.broadcast(this.id), JSON.stringify(fdc3Message));
    }

    public getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        if (contextType) {
            return new Promise<Context>((resolve, reject) => {
                const context = this.lastContexts.get(contextType);
                if(context) {
                    resolve(context);
                } else {
                    reject(new Error("The given contextType: " + contextType + " was not found in the saved contexts."));
                }
            });
        } else {
            return new Promise<Context>((resolve, reject) => {
                if(this.lastContext) {
                    resolve(this.lastContext);
                } else {
                    reject(new Error("The last saved context is undefined."));
                }
            });
        }
    }

    public addContextListener(contextType: string | null, handler: ContextHandler): Promise<Listener>;
    public addContextListener(handler: ContextHandler): Promise<Listener>;
    public async addContextListener(contextType: any, handler?: any): Promise<Listener> {
        if(typeof contextType != 'string'){
            throw new Error("addContextListener without contextType is deprecated, please use the newer version.");
        } else {
            const listener = new ComposeUIListener(this.messageRouterClient, handler, this.id, contextType);
            await listener.subscribe();
            return listener;
        };
    }
}