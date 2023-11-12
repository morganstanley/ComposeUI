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
import { AppMetadata, IntentResolution, IntentResult } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { ComposeUIChannel } from "./ComposeUIChannel";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3FindChannelRequest } from "./messages/Fdc3FindChannelRequest";
import { Fdc3FindChannelResponse } from "./messages/Fdc3FindChannelResponse";
import { Fdc3GetIntentResultRequest } from "./messages/Fdc3GetIntentResultRequest";
import { Fdc3GetIntentResultResponse } from "./messages/Fdc3GetIntentResultResponse";

export class ComposeUIIntentResolution implements IntentResolution {
    private messageRouterClient: MessageRouter;
    public source: AppMetadata;
    public intent: string

    constructor(messageRouterClient: MessageRouter, intent: string, source: AppMetadata) {
        this.intent = intent;
        this.source = source;
        this.messageRouterClient = messageRouterClient;
    }

    getResult(): Promise<IntentResult> {
        return new Promise(async(resolve, reject) => {
            const intentResolutionRequest = new Fdc3GetIntentResultRequest(this.intent, this.source, this.source.version);
            console.log(intentResolutionRequest);
            const response = await this.messageRouterClient.invoke(ComposeUITopic.getIntentResult(), JSON.stringify(intentResolutionRequest));
            if (!response) {
                return reject(ComposeUIErrors.NoAnswerWasProvided);
            } else {
                const result = <Fdc3GetIntentResultResponse>(JSON.parse(response));
                if (result.error) {
                    return reject(result.error);
                } else {
                    if (result.channelId && result.channelType) {
                        const message = JSON.stringify(new Fdc3FindChannelRequest(result.channelId, result.channelType));
                        const response = await this.messageRouterClient.invoke(ComposeUITopic.findChannel(), message);
                        if(response) {
                            const fdc3Message = <Fdc3FindChannelResponse>JSON.parse(response);
                            if(fdc3Message.error) {
                                return reject(fdc3Message.error);
                            } 
                            if (fdc3Message.found){
                                const channel = new ComposeUIChannel(result.channelId, result.channelType, this.messageRouterClient);
                                return resolve(channel);
                            }
                        }
                    } else if (result.context) {
                        return resolve(result.context);
                    }
                    return reject(ComposeUIErrors.NoAnswerWasProvided);
                }           
            }
        });
    }
}