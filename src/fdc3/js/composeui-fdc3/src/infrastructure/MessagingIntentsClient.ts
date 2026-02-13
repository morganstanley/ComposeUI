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
import { JsonMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { ChannelHandler } from "./ChannelHandler";
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

export class MessagingIntentsClient implements IntentsClient {
    private channelHandler: ChannelHandler;
    private jsonMessaging: JsonMessaging;

    constructor( jsonMessaging: JsonMessaging, channelFactory: ChannelHandler, ) {
        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) {
            throw new Error(ComposeUIErrors.InstanceIdNotFound);
        }

        this.channelHandler = channelFactory;
        this.jsonMessaging = jsonMessaging;
    }

    public async findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        const request = new Fdc3FindIntentRequest(window.composeui.fdc3.config!.instanceId!, intent, context, resultType);
        const message = await this.jsonMessaging.invokeJsonService<Fdc3FindIntentRequest, Fdc3FindIntentResponse>(ComposeUITopic.findIntent(), request);
        if (!message) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        if (message.error) {
            throw new Error(message.error);
        }
        else {
            return message.appIntent!;
        }
    }

    public async findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        const request = new Fdc3FindIntentsByContextRequest(window.composeui.fdc3.config!.instanceId!, context, resultType);
        const message = await this.jsonMessaging.invokeJsonService<Fdc3FindIntentsByContextRequest, Fdc3FindIntentsByContextResponse>(ComposeUITopic.findIntentsByContext(), request);
        if (!message) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        if (message.error) {
            throw new Error(message.error);
        }

        return message.appIntents!;
    }

    public async getIntentResolution(messageId: string, intent: string, source: AppMetadata): Promise<IntentResolution> {
        return new ComposeUIIntentResolution(messageId, this.jsonMessaging, this.channelHandler, intent, source);
    }

    public async raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        if (typeof app == 'string') {
            throw new Error(ComposeUIErrors.AppIdentifierTypeFailure);
        }

        const messageId = Math.floor(Math.random() * 10000);
        const message = new Fdc3RaiseIntentRequest(messageId, window.composeui.fdc3.config!.instanceId!, intent, context, app);
        const response = await this.jsonMessaging.invokeJsonService<Fdc3RaiseIntentRequest, Fdc3RaiseIntentResponse>(ComposeUITopic.raiseIntent(), message);
        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        if (response.error) {
            throw new Error(response.error);
        }

        const intentResolution = new ComposeUIIntentResolution(response.messageId, this.jsonMessaging, this.channelHandler, response.intent!, response.appMetadata!);
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
            context,
            app);

        const response = await this.jsonMessaging.invokeJsonService<Fdc3RaiseIntentForContextRequest, Fdc3RaiseIntentResponse>(ComposeUITopic.raiseIntentForContext(), request);
        if (!response) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        if (response.error) {
            throw new Error(response.error);
        }
        
        const intentResolution = new ComposeUIIntentResolution(response.messageId!, this.jsonMessaging, this.channelHandler, response.intent!, response.appMetadata!);
        return intentResolution;
    }
}
