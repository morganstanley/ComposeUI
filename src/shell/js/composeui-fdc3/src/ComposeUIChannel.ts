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
import { ComposeUIChannelType } from "./ComposeUIChannelType";
import { ComposeUIListener } from "./ComposeUIListener";
import { Fdc3ChannelMessageRouterMessage } from "./Fdc3ChannelMessageRouterMessage";

//TODO (For now we are working on just the user channel)
export class ComposeUIChannel implements Channel{
    id!: string;
    type!: "user" | "app" | "private";
    displayMetadata?: DisplayMetadata | undefined;

    private messageRouterClient!: MessageRouter;
    private lastContexts: Map<string, Context> = new Map<string, Context>();
    private lastContext?: Context;

    constructor(id: string /*topic*/, type: ComposeUIChannelType, messageRouterClient: MessageRouter){
        this.id = id; //"composeui/fdc3/v2.0/userchannels/{name}/"
        this.type = type; //USER
        this.messageRouterClient = messageRouterClient;
    }

    public broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;
        const fdc3Message = new Fdc3ChannelMessageRouterMessage(this.id + "broadcast", context);
        return this.messageRouterClient.publish(this.id + "broadcast", JSON.stringify(fdc3Message));
    }

    public getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        if (contextType != null && contextType != undefined) {
            return new Promise<Context>((resolve, reject) => {
                console.log("Resolving the current context: ", contextType);
                resolve(this.lastContexts.get(contextType)!);
            });
        } else {
            return new Promise<Context>((resolve, reject) => {
                console.log("Resolving the current context with the last context: ", this.lastContext);
                resolve(this.lastContext!);
            });
        }
    }

    public addContextListener(contextType: string | null, handler: ContextHandler): Promise<Listener>;
    public addContextListener(handler: ContextHandler): Promise<Listener>;
    public async addContextListener(contextType: any, handler?: any): Promise<Listener> {
        if(contextType === null 
            || contextType === undefined 
            || typeof contextType != 'string'){
            throw new Error("addContextListener without contextType is depracted, please use the newer version.");
        } else {
            const listener = new ComposeUIListener(this.messageRouterClient, handler, this.id, contextType);
            await listener.subscribe("broadcast");
            return listener;
        };
    }
}