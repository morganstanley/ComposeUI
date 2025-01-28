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

import { getCurrentChannel, OpenError } from "@finos/fdc3";
import { MessageRouterOpenClient } from "./infrastructure/MessageRouterOpenClient";
import { ComposeUIErrors } from "./infrastructure/ComposeUIErrors";
import { Fdc3OpenResponse } from "./infrastructure/messages/Fdc3OpenResponse";
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { Fdc3GetOpenedAppContextResponse } from "./infrastructure/messages/Fdc3GetOpenedAppContextResponse";
import { OpenAppIdentifier } from "./infrastructure/OpenAppIdentifier";
import { rejects } from "assert";

describe('MessageRouter OpenClient tests', () => {
    let client: MessageRouterOpenClient;
    let messageRouterClient: MessageRouter;
    const openAppIdentifier: OpenAppIdentifier = { openedAppContextId: "contextId" };

    beforeEach(() => {
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId: "test",
                openAppIdentifier: {
                    openedAppContextId: "contextId"
                }
            }
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(undefined) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
    });

    it('open throws AppNotFound error as AppIdentifier not set', async() => {
        await expect(client.open())
            .rejects
            .toThrow(OpenError.AppNotFound);
    });

    it('open throws NoAnswerWasProvided error as no response received by the MessageRouter server', async() => {
        await expect(client.open("appId1"))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('open throws error as error response received by the MessageRouter server', async() => {
        const response: Fdc3OpenResponse = {
            error: "testError"
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        await expect(client.open("appId1"))
            .rejects
            .toThrow("testError");
    });

    it('open returns AppIdentifier response received by the MessageRouter server', async() => {
        const response: Fdc3OpenResponse = {
            appIdentifier: {
                appId: "appId1",
                instanceId: "instanceId1"
            }
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        }

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        const result = await client.open({appId: "appId1"}, {type: "fdc3.instrument"});
        expect(result).toMatchObject({appId: "appId1", instanceId: "instanceId1"});
    });

    it('getOpenedAppContext throws error as context id is not set', async() => {
        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient);
        await expect(client.getOpenedAppContext())
            .rejects
            .toThrow("Context id is not defined on the window object.");
    });

    it('getOpenedAppContext throws NoAnswerWasProvided error as no response received from the MessageRouter server', async() => {
        await expect(client.getOpenedAppContext())
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('getOpenedAppContext throws NoAnswerWasProvided error as not correct response received from the MessageRouter server', async() => {
        const response = {
            payload: "wrongPayload"
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        await expect(client.getOpenedAppContext())
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('getOpenedAppContext throws error as error response received from the MessageRouter server', async() => {
        const response: Fdc3GetOpenedAppContextResponse = {
            error: "testGetOpenedAppContextError"
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        await expect(client.getOpenedAppContext())
            .rejects
            .toThrow("testGetOpenedAppContextError");
    });

    it('getOpenedAppContext throws NoAnswerWasProvided error as no context response received from the MessageRouter server', async() => {
        const response: Fdc3GetOpenedAppContextResponse = { };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        await expect(client.getOpenedAppContext())
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('getOpenedAppContext returns context', async() => {
        const response: Fdc3GetOpenedAppContextResponse = {
            context: {type: "fdc3.instrument"}
        };

        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({ unsubscribe: () => { } });
            }),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`) })
        };

        client = new MessageRouterOpenClient("fdc3InstanceId", messageRouterClient, openAppIdentifier);
        const result = await client.getOpenedAppContext();

        expect(result.type).toBe("fdc3.instrument");
    });
});