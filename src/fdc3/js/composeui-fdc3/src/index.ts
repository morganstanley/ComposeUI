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
import { IMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { OpenAppIdentifier } from "./infrastructure/OpenAppIdentifier";

declare global {
    interface Window {
        composeui: {
            fdc3: {
                config: AppIdentifier | undefined;
                channelId : string | undefined;
                openAppIdentifier: OpenAppIdentifier | undefined;
            },
            messaging: {
                communicator: IMessaging | undefined;
            }
        }
        fdc3: DesktopAgent;
    }
}

async function initialize(): Promise<void> {
    //TODO: decide if we want to join to a channel by default.
    let channelId: string | undefined = window.composeui.fdc3.channelId;
    const openAppIdentifier: OpenAppIdentifier | undefined = window.composeui.fdc3.openAppIdentifier;
    const messaging = window.composeui.messaging.communicator as IMessaging;
    const fdc3 = new ComposeUIDesktopAgent(messaging);
    await fdc3.init();

    let _disposed = false;

    const disposeAgent = () => {
        if (_disposed) {
            return;
        }

        _disposed = true;

        try {
            const agent: ComposeUIDesktopAgent = (window.fdc3 as ComposeUIDesktopAgent) || fdc3;
            if (agent) {
                agent[Symbol.asyncDispose]()
            }
        } catch (err) {
            console.warn("Error disposing FDC3 agent", err);
        } finally {
            // remove handlers after first run
            window.removeEventListener("beforeunload", disposeAgent);
            window.removeEventListener("unload", disposeAgent);
        }
    };

    window.addEventListener("beforeunload", disposeAgent);
    window.addEventListener("unload", disposeAgent);

    if (channelId) {
        await fdc3.joinUserChannel(channelId)
            .then(async() => {
                if (openAppIdentifier) {
                    await fdc3.getOpenedAppContext()
                        .then(() => {
                            window.fdc3 = fdc3;
                            console.log("FDC3 initialized, handled initial context which initiates that the app was opened via `fdc3.open` and joined to channel: ", channelId, window.fdc3);
                            window.dispatchEvent(new Event("fdc3Ready"));
                        })
                } else {
                    window.fdc3 = fdc3;
                    console.log("FDC3 initialized and joined to channel: ", channelId, window.fdc3);
                    window.dispatchEvent(new Event("fdc3Ready"));
                }
            });
    } else {
        if (openAppIdentifier) {
            await fdc3.getOpenedAppContext().then(() => {
                window.fdc3 = fdc3;
                console.log("FDC3 initialized, handled initial context which initiates that the app was opened via `fdc3.open`: ", window.fdc3);
                window.dispatchEvent(new Event("fdc3Ready"));
            })
        } else {
            window.fdc3 = fdc3;
            console.log("FDC3 initialized: ", window.fdc3);
            window.dispatchEvent(new Event("fdc3Ready"));
        }
    }
}

initialize();
