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
 */

import { MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { ChannelSelectorResponse } from "./messages/ChannelSelectorResponse";
import { ChannelSelectorRequest } from "./messages/ChannelSelectorRequest"

export class ChannelSelectorClient {
    constructor(private readonly messageRouterClient: MessageRouter,  private readonly instanceId: string, private readonly channelId: string){
    
    }

    public async subscribe() : Promise<string>{
        await this.messageRouterClient.connect();
        await this.messageRouterClient.subscribe("ComposeUI/fdc3/v2.0/changeChannel", (topicMessage: TopicMessage) => {
            const payload = <ChannelSelectorResponse>JSON.parse(topicMessage.payload!); //todo check parsing as lowercase doesn't work
            
            if(payload.instanceId === this.instanceId)
            {
                window.fdc3.joinUserChannel(payload.channelId);
            }
        });
        return this.channelId!;
    }

    public async colorUpdate(channelId: string, channelColor: string | undefined ) : Promise<void | undefined>{       
        const message = JSON.stringify(new ChannelSelectorRequest(
            channelId,
            this.instanceId,
            channelColor
       ));

       await this.messageRouterClient.publish(`ComposeUI/fdc3/v2.0/channelSelectorColor-${this.instanceId}`, message);
    }
}