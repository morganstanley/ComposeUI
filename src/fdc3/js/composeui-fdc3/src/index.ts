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



/*
import { ComposeUIDesktopAgent } from "./ComposeUIDesktopAgent";
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";


let fdc3 = new ComposeUIDesktopAgent("default", createMessageRouter());

export default fdc3;

*/

import { createAgent } from '@connectifi/agent-web';

async function agent() {
    window.addEventListener('DOMContentLoaded', async() => {
    const appId = '*@sandbox';
    const interopEndpoint = 'https://dev.connectifi-interop.com';
    (window as any).fdc3 = await createAgent(interopEndpoint, appId);
    console.log("!!!");
    });
}
//(window as any).fdc3 = agent();

//let a = agent();
//
agent();
//export default a;
//export default agent();
