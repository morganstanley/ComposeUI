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

import { AppIdentifier, AppIntent, AppMetadata, Context, IntentResolution } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { ChannelFactory } from "./ChannelFactory";
import { ComposeUIErrors } from "./ComposeUIErrors";
import { ComposeUIIntentResolution } from "./ComposeUIIntentResolution";
import { ComposeUITopic } from "./ComposeUITopic";
import { IntentsClient } from "./IntentsClient";
import { Fdc3FindIntentRequest } from "./messages/Fdc3FindIntentRequest";
import { Fdc3FindIntentResponse } from "./messages/Fdc3FindIntentResponse";
import { Fdc3FindIntentsByContextRequest } from "./messages/Fdc3FindIntentsByContextRequest";
import { Fdc3FindIntentsByContextResponse } from "./messages/Fdc3FindIntentsByContextResponse";
import { Fdc3RaiseIntentRequest } from "./messages/Fdc3RaiseIntentRequest";
import { Fdc3RaiseIntentResponse } from "./messages/Fdc3RaiseIntentResponse";
import { Fdc3RaiseIntentForContextRequest } from "./messages/Fdc3RaiseIntentForContextRequest";

export class MessageRouterIntentsClient implements IntentsClient {

    private messageRouterClient: MessageRouter;
    private channelFactory: ChannelFactory;

    constructor(messageRouterClient: MessageRouter, channelFactory: ChannelFactory) {
        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) {
            throw new Error(ComposeUIErrors.InstanceIdNotFound);
        }

        this.messageRouterClient = messageRouterClient;
        this.channelFactory = channelFactory;
    }

    public async findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        const request = new Fdc3FindIntentRequest(window.composeui.fdc3.config!.instanceId!, intent, context, resultType);
        const message = await this.messageRouterClient.invoke(ComposeUITopic.findIntent(), JSON.stringify(request));
        if (!message) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const findIntentResponse = <Fdc3FindIntentResponse>JSON.parse(message);
        if (findIntentResponse.error) {
            throw new Error(findIntentResponse.error);
        }
        else {
            return findIntentResponse.appIntent!;
        }
    }

    public async findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        const request = new Fdc3FindIntentsByContextRequest(window.composeui.fdc3.config!.instanceId!, context, resultType);
        const message = await this.messageRouterClient.invoke(ComposeUITopic.findIntentsByContext(), JSON.stringify(request));
        if (!message) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const findIntentsByContextResponse = <Fdc3FindIntentsByContextResponse>JSON.parse(message);
        if (findIntentsByContextResponse.error) {
            throw new Error(findIntentsByContextResponse.error);
        }

        return findIntentsByContextResponse.appIntents!;
    }

    public async getIntentResolution(messageId: string, intent: string, source: AppMetadata): Promise<IntentResolution> {
        return new ComposeUIIntentResolution(messageId, this.messageRouterClient, this.channelFactory, intent, source);
    }

    public async raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        if (typeof app == 'string') {
            throw new Error(ComposeUIErrors.AppIdentifierTypeFailure);
        }

        const messageId = Math.floor(Math.random() * 10000);
        const message = new Fdc3RaiseIntentRequest(messageId, window.composeui.fdc3.config!.instanceId!, intent, context, app);
        const responseFromService = await this.messageRouterClient.invoke(ComposeUITopic.raiseIntent(), JSON.stringify(message));
        if (!responseFromService) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const response = <Fdc3RaiseIntentResponse>JSON.parse(responseFromService);

        if (response.error) {
            throw new Error(response.error);
        }

        const intentResolution = new ComposeUIIntentResolution(response.messageId, this.messageRouterClient, this.channelFactory, response.intent!, response.appMetadata!);
        return intentResolution;
    }

    public async raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        if (typeof app == "string") {
            throw new Error(ComposeUIErrors.AppIdentifierTypeFailure);
        }

        const messageId = Math.floor(Math.random() * 10000);
        const request = new Fdc3RaiseIntentForContextRequest(
            messageId,
            window.composeui.fdc3.config!.instanceId!,
            JSON.stringify(context),
            app);

        const responseFromService = await this.messageRouterClient.invoke(ComposeUITopic.raiseIntentForContext(), JSON.stringify(request));
        if (!responseFromService) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        const response = <Fdc3RaiseIntentResponse>JSON.parse(responseFromService);
        if (response.error) {
            throw new Error(response.error);
        }
        
        const intentResolution = new ComposeUIIntentResolution(response.messageId!, this.messageRouterClient, this.channelFactory, response.intent!, response.appMetadata!);
        return intentResolution;
    }
}