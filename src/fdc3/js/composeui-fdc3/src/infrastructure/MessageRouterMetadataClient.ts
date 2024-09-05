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

import { AppIdentifier, AppMetadata, ImplementationMetadata } from "@finos/fdc3";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { MetadataClient } from "./MetadataClient";
import { Fdc3GetInfoRequest } from "./messages/Fdc3GetInfoRequest";
import { Fdc3GetInfoResponse } from "./messages/Fdc3GetInfoResponse";
import { Fdc3FindInstancesRequest } from "./messages/Fdc3FindInstancesRequest";
import { Fdc3FindInstancesResponse } from "./messages/Fdc3FindInstancesResponse";
import { Fdc3GetAppMetadataRequest } from "./messages/Fdc3GetAppMetadataRequest";
import { Fdc3GetAppMetadataResponse } from "./messages/Fdc3GetAppMetadataResponse";
import { ComposeUITopic } from "./ComposeUITopic";
import { ComposeUIErrors } from "./ComposeUIErrors";

export class MessageRouterMetadataClient implements MetadataClient {
    constructor(private messageRouterClient: MessageRouter, private appIdentifier: AppIdentifier) { }

    public async getInfo(): Promise<ImplementationMetadata> {
        var request = new Fdc3GetInfoRequest(this.appIdentifier);
        var payload = await this.messageRouterClient.invoke(ComposeUITopic.getInfo(), JSON.stringify(request));

        if (!payload) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        var response = <Fdc3GetInfoResponse>JSON.parse(payload);
        if (response.error) {
            throw new Error(response.error);
        }

        return response.implementationMetadata!;
    }

    public async findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        var request = new Fdc3FindInstancesRequest(this.appIdentifier.instanceId!, app);
        var payload = await this.messageRouterClient.invoke(ComposeUITopic.findInstances(), JSON.stringify(request));

        if (!payload) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        var response = <Fdc3FindInstancesResponse>JSON.parse(payload);
        if (response.error) {
            throw new Error(response.error);
        }

        return response.instances!;
    }

    public async getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        var request = new Fdc3GetAppMetadataRequest(this.appIdentifier.instanceId!, app);
        var payload = await this.messageRouterClient.invoke(ComposeUITopic.getAppMetadata(), JSON.stringify(request));

        if (!payload) {
            throw new Error(ComposeUIErrors.NoAnswerWasProvided);
        }

        var response = <Fdc3GetAppMetadataResponse>JSON.parse(payload);
        if (response.error) {
            throw new Error(response.error);
        }

        return response.appMetadata!;
    }
}