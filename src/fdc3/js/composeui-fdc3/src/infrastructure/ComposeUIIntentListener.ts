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

import { IntentHandler, Listener, Context, Channel, ResultError } from "@finos/fdc3";
import { MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
import { Unsubscribable } from "rxjs";
import { ComposeUITopic } from "./ComposeUITopic";
import { Fdc3RaiseIntentResolutionRequest } from "./messages/Fdc3RaiseIntentResolutionRequest";
import { Fdc3StoreIntentResultRequest } from "./messages/Fdc3StoreIntentResultRequest";
import { Fdc3StoreIntentResultResponse } from "./messages/Fdc3StoreIntentResultResponse";
import { Fdc3IntentListenerRequest } from "./messages/Fdc3IntentListenerRequest";
import { Fdc3IntentListenerResponse } from "./messages/Fdc3IntentListenerResponse";
import { ComposeUIErrors } from "./ComposeUIErrors";

export class ComposeUIIntentListener implements Listener {
    private unsubscribable?: Unsubscribable;
    private isSubscribed: boolean = false;

    constructor(
        private messageRouterClient: MessageRouter,
        private intent: string,
        private instanceId: string,
        private intentHandler: IntentHandler) {
    }

    public async registerIntentHandler(): Promise<void> {
        const topic = ComposeUITopic.raiseIntent(this.intent, this.instanceId);

        //Applications register the intents and context data combinations they support in the App Directory.
        //https://fdc3.finos.org/docs/intents/spec
        this.unsubscribable = await this.messageRouterClient.subscribe(
            topic,
            async (topicMessage: TopicMessage) => {
                const message = <Fdc3RaiseIntentResolutionRequest>(JSON.parse(topicMessage.payload!));
                //TODO: integrationtest
                let request: Fdc3StoreIntentResultRequest;
                try {
                    const result = this.intentHandler(message.context, message.contextMetadata);
                    if (result && result instanceof Promise) {
                        const intentResult = <object>await result;
                        if (!intentResult) {
                            request = new Fdc3StoreIntentResultRequest(message.messageId, this.intent, this.instanceId, message.contextMetadata.source.instanceId!, undefined, undefined, undefined, true);
                        } else if ('id' in intentResult) {
                            const channel = <Channel>intentResult;
                            request = new Fdc3StoreIntentResultRequest(message.messageId, this.intent, this.instanceId, message.contextMetadata.source.instanceId!, channel.id, channel.type);
                        } else if ('type' in intentResult) {
                            const context = <Context>intentResult;
                            request = new Fdc3StoreIntentResultRequest(message.messageId, this.intent, this.instanceId, message.contextMetadata.source.instanceId!, undefined, undefined, context);
                        } else {
                            throw new Error("Cannot detect the return type of the IntentHandler.");
                        }
                    } else {
                        request = new Fdc3StoreIntentResultRequest(message.messageId, this.intent, this.instanceId, message.contextMetadata.source.instanceId!, undefined, undefined, undefined, true);
                    }
                } catch (error) {
                    console.error(error);
                    request = new Fdc3StoreIntentResultRequest(message.messageId, this.intent, this.instanceId, message.contextMetadata.source.instanceId!, undefined, undefined, undefined, false, ResultError.IntentHandlerRejected);
                }

                const result = await this.messageRouterClient.invoke(ComposeUITopic.sendIntentResult(), JSON.stringify(request));
                if (!result) {
                    return;
                } else {
                    const response = <Fdc3StoreIntentResultResponse>(JSON.parse(result));
                    if (response.error || !response.stored) {
                        console.log("Error while resolving the intent.", response.error);
                        throw new Error(response.error);
                    }
                }
            });

        this.isSubscribed = true;
    }

    public unsubscribe(): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            if (!this.isSubscribed) return;
            const message = new Fdc3IntentListenerRequest(this.intent, this.instanceId, "Unsubscribe");
            const response = await this.messageRouterClient.invoke(ComposeUITopic.addIntentListener(), JSON.stringify(message));
            if (!response) {
                return reject(ComposeUIErrors.NoAnswerWasProvided);
            } else {
                const result = <Fdc3IntentListenerResponse>JSON.parse(response);
                if (result.error) {
                    return reject(result.error);
                } else if (result.stored) {
                    return reject(ComposeUIErrors.UnsubscribeFailure);
                } else {
                    this.unsubscribable?.unsubscribe();
                    this.isSubscribed = false;
                    return resolve();
                }
            }
        });
    }
}