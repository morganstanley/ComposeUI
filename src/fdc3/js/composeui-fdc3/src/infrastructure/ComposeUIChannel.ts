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
import { JsonMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { ChannelType } from "./ChannelType";
import { ComposeUIContextListener } from "./ComposeUIContextListener";
import { Fdc3GetCurrentContextRequest } from "./messages/Fdc3GetCurrentContextRequest";
import { ComposeUITopic } from "./ComposeUITopic";

export class ComposeUIChannel implements Channel {
    id: string;
    type: "user" | "app" | "private";
    displayMetadata?: DisplayMetadata;

    protected jsonMessaging: JsonMessaging;
    private lastContexts: Map<string, Context> = new Map<string, Context>();
    private lastContext?: Context;
    private openHandled: boolean = true; //by default true so if a channel was created then it has no effect

    constructor(id: string, type: ChannelType, jsonMessaging: JsonMessaging, displayMetadata?: DisplayMetadata) {
        this.id = id;
        this.type = type;
        this.jsonMessaging = jsonMessaging;
        this.displayMetadata = displayMetadata;
    }

    //Broadcasting on the composeui/fdc3/v2.0/<channel>/broadcast topic
    public async broadcast(context: Context): Promise<void> {
        //Setting the last published context message.
        this.lastContexts.set(context.type, context);
        this.lastContext = context;
        const topic = ComposeUITopic.broadcast(this.id, this.type);
        await this.jsonMessaging.publishJson(topic, context);
    }

    public async getCurrentContext(contextType?: string | undefined): Promise<Context | null> {
        const message = new Fdc3GetCurrentContextRequest(contextType);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3GetCurrentContextRequest, Context>(ComposeUITopic.getCurrentContext(this.id, this.type), message);
        if (response) {
            if (response) {
                this.lastContext = response;
                this.lastContexts.set(response.type, response);
            }
        }
        return this.retrieveCurrentContext(contextType);
    }

    private retrieveCurrentContext(contextType?: string): Context | null {
        let context;
        if (contextType) {
            context = this.lastContexts.get(contextType);
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

        const listener = new ComposeUIContextListener(this.openHandled, this.jsonMessaging, handler, contextType);
        await listener.subscribe(this.id, this.type);
        return listener;
    }

    public setOpenHandled(openHandled: boolean): void {
        this.openHandled = openHandled;
    }
}