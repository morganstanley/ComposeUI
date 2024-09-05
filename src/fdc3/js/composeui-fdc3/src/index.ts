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


import { AppIdentifier, DesktopAgent } from "@finos/fdc3";
import { ComposeUIDesktopAgent } from "./ComposeUIDesktopAgent";
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";


declare global {
    interface Window {
        composeui: {
            fdc3: {
                config: AppIdentifier | undefined;
                channelId : string | undefined;
            }
        }
        fdc3: DesktopAgent;
    }
}

async function initialize(): Promise<void> {
    //TODO: decide if we want to join to a channel by default.
    let channelId: string | undefined = window.composeui.fdc3.channelId;
    let fdc3 = new ComposeUIDesktopAgent(createMessageRouter());

    if (channelId) {
        await fdc3.joinUserChannel(channelId)
            .then(() => {
                window.fdc3 = fdc3;
                window.dispatchEvent(new Event("fdc3Ready"));
            });
    } else {
        window.fdc3 = fdc3;
        window.dispatchEvent(new Event("fdc3Ready"));
    }
}

initialize();
