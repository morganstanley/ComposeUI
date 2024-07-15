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

import { Listener } from "@finos/fdc3";

export class PrivateChannelContextListenerEventListener implements Listener {

    private readonly handler: ((contextType?: string) => void);
    private readonly unsubscribeCallback: (x: PrivateChannelContextListenerEventListener) => void;
    private subscribed: boolean;

    constructor(handler: ((contextType?: string) => void), onUnsubscribe: (x: PrivateChannelContextListenerEventListener) => void) {
        this.handler = handler;
        this.subscribed = true;
        this.unsubscribeCallback = onUnsubscribe;
    }

    public execute(contextType?: string) {
        if (!this.subscribed) {
            return;
        }
        this.handler(contextType);
    }

    public unsubscribe(): void {
        this.unsubscribeInternal(true);
    }

    public unsubscribeInternal(doCallback: boolean): void {
        this.subscribed = false;
        if (doCallback) {
            this.unsubscribeCallback(this);
        }
    }
}