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
import { Fdc3FindInstancesResponse } from "./infrastructure/messages/Fdc3FindInstancesResponse";
import { Fdc3GetAppMetadataResponse } from "./infrastructure/messages/Fdc3GetAppMetadataResponse";

describe('MessageRouterMetadataClient tests', () => {
    beforeEach(() => {
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId: "test",
                openAppIdentifier: {
                    openedAppContextId: "test"
                }
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

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
                UserChannelMembershipAPIs: true
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        var result = await desktopAgent.getInfo();
        expect(result).toMatchObject(implementationMetadata);
    });


    it('findInstances returns no payload', async () => {
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        await expect(desktopAgent.findInstances({appId: "test"}))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('findInstances returns error', async() => {
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        await expect(desktopAgent.findInstances({appId: "test"}))
            .rejects
            .toThrow('dummyError');
    });

    it('findInstances return instances', async() => {

        const response : Fdc3FindInstancesResponse = {
            instances: [{appId: "test", instanceId: "id"}]
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        var result = await desktopAgent.findInstances({appId: "test"});
        expect(result).toMatchObject([{appId: "test", instanceId: "id"}]);
    });

    it('getAppMetadata returns no payload', async () => {
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        await expect(desktopAgent.getAppMetadata({appId: "test"}))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('getAppMetadata returns error', async() => {
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        await expect(desktopAgent.getAppMetadata({appId: "test"}))
            .rejects
            .toThrow('dummyError');
    });

    it('getAppMetadata return instances', async() => {

        const response : Fdc3GetAppMetadataResponse = {
            appMetadata: { appId: "test" }
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

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);

        var result = await desktopAgent.getAppMetadata({appId: "test"});
        expect(result).toMatchObject({appId: "test"});
    });
});