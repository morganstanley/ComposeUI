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
import { ComposeUITopic } from "./ComposeUITopic";
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
            const response = <Fdc3GetIntentResultResponse>await this.messageRouterClient.invoke(ComposeUITopic.getIntentResult(), JSON.stringify(intentResolutionRequest));
            if (!response) {
                reject("No answer came from the server whe resolving intent");
            } else if (response.error) {
                reject(response.error);
            } else {
                //TODO(Lilla): context? channel?
                //TODO(Lilla): we should return just when the intentHandler resolved the intent.
                //TODO(Lilla): mesage should be defined as some error might happens and we need to rejct the promise in that case.
                resolve(response.intentResult);
            }           
        });
    }
}