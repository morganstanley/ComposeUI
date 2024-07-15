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
import { ChannelFactory } from "./ChannelFactory";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3GetIntentResultRequest } from "./messages/Fdc3GetIntentResultRequest";
import { Fdc3GetIntentResultResponse } from "./messages/Fdc3GetIntentResultResponse";

export class ComposeUIIntentResolution implements IntentResolution {
    private messageRouterClient: MessageRouter;
    private channelFactory: ChannelFactory;
    public source: AppMetadata;
    public intent: string
    public messageId: string;


    constructor(messageId: string, messageRouterClient: MessageRouter, channelFactory: ChannelFactory, intent: string, source: AppMetadata) {
        this.messageId = messageId;
        this.intent = intent;
        this.source = source;
        this.messageRouterClient = messageRouterClient;
        this.channelFactory = channelFactory;
    }

    async getResult(): Promise<IntentResult> {
        const intentResolutionRequest = new Fdc3GetIntentResultRequest(this.messageId, this.intent, this.source, this.source.version);
        const response = await this.messageRouterClient.invoke(ComposeUITopic.getIntentResult(), JSON.stringify(intentResolutionRequest));
        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }
        const result = <Fdc3GetIntentResultResponse>(JSON.parse(response));
        if (result.error) {
            throw new Error(result.error);
        }
        if (result.channelId && result.channelType) {
            const channel = this.channelFactory.getChannel(result.channelId, result.channelType)
            return channel;
        } else if (result.context) {
            return result.context;
        } else if (result.voidResult) {
            console.log("The IntentListener returned void. ", result.voidResult);
            return;
        }
        throw new Error(ComposeUIErrors.NoAnswerWasProvided);
    }
}