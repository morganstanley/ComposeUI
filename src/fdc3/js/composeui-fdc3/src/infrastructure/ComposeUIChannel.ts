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
import { ComposeUIContextListener } from "./ComposeUIContextListener";
import { Fdc3GetCurrentContextRequest } from "./messages/Fdc3GetCurrentContextRequest";
import { ComposeUITopic } from "./ComposeUITopic";

export class ComposeUIChannel implements Channel {
    id: string;
    type: "user" | "app" | "private";
    displayMetadata?: DisplayMetadata;

    private messageRouterClient: MessageRouter;
    private lastContexts: Map<string, Context> = new Map<string, Context>();
    private lastContext?: Context;

    constructor(id: string, type: ChannelType, messageRouterClient: MessageRouter) {
        this.id = id;
        this.type = type;
        this.messageRouterClient = messageRouterClient;
    }

    //Broadcasting on the composeui/fdc3/v2.0/broadcast topic
    public async broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;
        const topic = ComposeUITopic.broadcast(this.id, this.type);
        await this.messageRouterClient.publish(topic, JSON.stringify(context));
    }

    //TODO add error
    public getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        return new Promise<Context | null>(async (resolve, reject) => {
            const message = JSON.stringify(new Fdc3GetCurrentContextRequest(contextType));
            const response = await this.messageRouterClient.invoke(ComposeUITopic.getCurrentContext(this.id, this.type), message);
            if (response) {
                const context = <Context>JSON.parse(response);
                if(context) {
                    this.lastContext = context;
                    this.lastContexts.set(context.type, context);
                    resolve(context);
                }
            }
            
            resolve(this.retrieveCurrentContext(contextType));
        });
    }

    public retrieveCurrentContext(contextType?: string): Context | null {
        let context;
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
        if (contextType != null && typeof contextType != 'string') {            
            handler = contextType;
            contextType = null;
        }
        
        const listener = new ComposeUIContextListener(this.messageRouterClient, handler, this.id, this.type, contextType);
        await listener.subscribe();
        return listener;
    }
}