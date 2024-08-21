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

import { ImplementationMetadata } from "@finos/fdc3";
import { ComposeUIDesktopAgent } from "./ComposeUIDesktopAgent";
import { ComposeUIErrors } from "./infrastructure/ComposeUIErrors";
import { Fdc3GetInfoResponse } from "./infrastructure/messages/Fdc3GetInfoResponse";

describe('MessageRouterMetadataClient tests', () => {
    beforeEach(() => {
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId: "test"
            }
        };
    });

    it('getInfo returns no payload', async () => {
        const messageRouterClientMock = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(undefined) }) 
        };

        const desktopAgent = new ComposeUIDesktopAgent("testChannelId", messageRouterClientMock);

        await expect(desktopAgent.getInfo())
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('getInfo returns error', async() => {
        const messageRouterClientMock = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify({ error: "dummyError" })}`) }) 
        };

        const desktopAgent = new ComposeUIDesktopAgent("testChannelId", messageRouterClientMock);

        await expect(desktopAgent.getInfo())
            .rejects
            .toThrow('dummyError');
    });

    it('getInfo returns ImplemetationMetadata', async() => {
        const implementationMetadata: ImplementationMetadata = {
            fdc3Version: '2.0.0',
            provider: 'ComposeUI',
            providerVersion: '1.0.0',
            optionalFeatures: {
                OriginatingAppMetadata: false,
                UserChannelMembershipAPIs: true,
                DesktopAgentBridging: false
            },
            appMetadata: {
                appId: 'dummyAppId'
            }
        };

        const response : Fdc3GetInfoResponse = {
            implementationMetadata: implementationMetadata
        };

        const messageRouterClientMock = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) }) 
        };

        const desktopAgent = new ComposeUIDesktopAgent("testChannelId", messageRouterClientMock);

        var result = await desktopAgent.getInfo();
        expect(result).toMatchObject(implementationMetadata);
    });
});