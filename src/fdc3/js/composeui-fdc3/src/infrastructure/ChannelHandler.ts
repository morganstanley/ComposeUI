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

import { Channel, ContextHandler, IntentHandler, Listener, PrivateChannel } from "@finos/fdc3";
import { ChannelType } from "./ChannelType";

export interface ChannelHandler extends AsyncDisposable {
    /*
    * Gets a channel by sending a request to the backend using its ID and type
    */
    getChannel(channelId: string, channelType: ChannelType): Promise<Channel>;

    /*
    * Creates a private channel by sending a request to the backend
    */
    createPrivateChannel(): Promise<PrivateChannel>;

    /*
    * Creates an app channel by sending a request to the backend using its ID
    */
    createAppChannel(channelId: string): Promise<Channel>;

    /*
    * Joins a user channel by sending a request to the backend using its ID
    */
    joinUserChannel(channelId: string): Promise<Channel>;

    /*
    * Gets all the user channels by sending a request to the backend
    */
    getUserChannels(): Promise<Channel[]>;

    /*
    * Gets all the app channels by sending a request to the backend
    */
    getIntentListener(intent: string, handler: IntentHandler): Promise<Listener>;

    /*
    * Gets a context listener by sending a request to the backend. This should reflect if the initial context sent by the fdc3.open call was handled or not.
    */
    getContextListener(openHandled: boolean, channel?: Channel, handler?: ContextHandler, contextType?: string | null): Promise<Listener>;

    /*
    * Configures the channel selector to allow the user to select a channel from the UI, by registering an endpoint to listen to UI initiated actions.
    */
    configureChannelSelectorFromUI(): Promise<void>;

    /*
    * Leaves the current channel by sending a request to the backend using its ID
    */
    leaveCurrentChannel(): Promise<void>;
}