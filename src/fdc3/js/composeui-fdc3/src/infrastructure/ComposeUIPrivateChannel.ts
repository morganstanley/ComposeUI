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

import { Listener, PrivateChannel } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { ComposeUIChannel } from "./ComposeUIChannel";
import { ComposeUIPrivateChannelSubscriptionEventListener } from "./ComposeUIPrivateChannelSubscriptionEventListener";

export class ComposeUIPrivateChannel extends ComposeUIChannel implements PrivateChannel {
    private addContextListenerHandlers: Array<ComposeUIPrivateChannelSubscriptionEventListener> = []

    constructor(id: string, messageRouterClient: MessageRouter) {
        super(id, "private", messageRouterClient);
    }
    onAddContextListener(handler: (contextType?: string) => void): Listener {
        var listener = new ComposeUIPrivateChannelSubscriptionEventListener(handler, (x)=>this.removeAddContextListenerHandler(x));
        this.addContextListenerHandlers.push(listener);
        return listener;
    }
    onUnsubscribe(handler: (contextType?: string) => void): Listener {
        throw new Error("Method not implemented.");
    }
    onDisconnect(handler: () => void): Listener {
        throw new Error("Method not implemented.");
    }
    disconnect(): void {
        throw new Error("Method not implemented.");
    }

    private removeAddContextListenerHandler(listener: ComposeUIPrivateChannelSubscriptionEventListener){
        var idx=this.addContextListenerHandlers.indexOf(listener);   
    this.addContextListenerHandlers.splice(idx)
    }
}