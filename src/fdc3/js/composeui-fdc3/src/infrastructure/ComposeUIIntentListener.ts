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

import { IntentHandler, Listener } from "@finos/fdc3";
import { MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { Unsubscribable } from "rxjs";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3RaiseIntentResolutionRequest } from "./messages/Fdc3RaiseIntentResolutionRequest";
import { Fdc3StoreIntentResultRequest } from "./messages/Fdc3StoreIntentResultRequest";
import { Fdc3StoreIntentResultResponse } from "./messages/Fdc3StoreIntentResultResponse";

export class ComposeUIIntentListener implements Listener {
    private unsubscribable?: Unsubscribable;
    private isSubscribed: boolean = false;

    constructor(
        private messageRouterClient: MessageRouter, 
        private intent: string, 
        private intentHandler: IntentHandler) {
    }

    public async registerIntentHandler(instanceId: string): Promise<void> {
        //Applications register the intents and context data combinations they support in the App Directory.
        //https://fdc3.finos.org/docs/intents/spec
        this.unsubscribable = await this.messageRouterClient.subscribe(
            ComposeUITopic.addIntentListener(this.intent, instanceId),
            async (topicMessage: TopicMessage) => {
                const message = <Fdc3RaiseIntentResolutionRequest> JSON.parse(topicMessage.payload!);
                //TODO: integrationtest
                let intentResult = await this.intentHandler(message.context, message.metadata);
                const request = new Fdc3StoreIntentResultRequest(this.intent, message.metadata?.source.instanceId!, intentResult);
                const response = <Fdc3StoreIntentResultResponse>await this.messageRouterClient.invoke(ComposeUITopic.sendIntentResult(), JSON.stringify(request));
                if (response.error || !response.stored) {
                    console.log("Error while resolving the intent.", response.error);
                }
            });

        this.isSubscribed = true;
    }

    public unsubscribe(): void {
        if (!this.isSubscribed) return;
        this.unsubscribable?.unsubscribe();
        this.isSubscribed = false;
    }
}