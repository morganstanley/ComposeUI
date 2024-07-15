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

export class PrivateChannelDisconnectEventListener implements Listener {

    private readonly handler: (() => void);
    private readonly unsubscribeCallback: (x: PrivateChannelDisconnectEventListener) => void;
    private subscribed: boolean;

    constructor(handler: (() => void), onUnsubscribe: (x:PrivateChannelDisconnectEventListener) => void) {
        this.handler = handler;
        this.subscribed = true;
        this.unsubscribeCallback = onUnsubscribe;
    }

    public execute() {
        if (!this.subscribed) {
            return;
        }
        this.handler();
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