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

import { jest } from '@jest/globals';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { AppIdentifier, Channel, ChannelError, Context, IntentHandler, Listener, PrivateChannel } from '@finos/fdc3';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { Fdc3FindIntentsByContextResponse } from './infrastructure/messages/Fdc3FindIntentsByContextResponse';
import { Fdc3RaiseIntentResponse } from './infrastructure/messages/Fdc3RaiseIntentResponse';
import { ComposeUIIntentListener } from './infrastructure/ComposeUIIntentListener';
import { ChannelType } from './infrastructure/ChannelType';

describe("Tests for ComposeUIDesktopAgent's intent handling", () => {

    beforeEach(() => {
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId : "test"
            }
        };
    });

    it('findIntent will throw error as no answer was provided by the DesktopAgent backend', async () => {
        const messageRouterClientMock = {
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

        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.findIntent("testIntent"))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('findIntent will throw error as it got error from the DesktopAgent backend', async () => {
        const fdc3IntentResponse = {
            appIntent: [],
            error: "Error happens..."
        };
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(fdc3IntentResponse)}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.findIntent("testIntent"))
            .rejects
            .toThrow("Error happens...");

        expect(messageRouterClientMock.invoke).toHaveBeenCalledWith(ComposeUITopic.findIntent(), JSON.stringify({ fdc3InstanceId: window?.composeui?.fdc3.config?.instanceId, intent: "testIntent" }));
    });

    it('findIntent will return AppIntent', async () => {
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify({ appIntent: { itent: "dummyIntent", apps: [{ appId: "appId1" }, { appId: "appdId2" }] } })}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        var resultAppIntent = await testDesktopAgent.findIntent("dummyIntent");
        expect(messageRouterClientMock.invoke).toHaveBeenCalledWith(ComposeUITopic.findIntent(), JSON.stringify({ fdc3InstanceId: window?.composeui?.fdc3.config?.instanceId, intent: "dummyIntent" }));
        expect(resultAppIntent).toMatchObject({ itent: "dummyIntent", apps: [{ appId: "appId1" }, { appId: "appdId2" }] });
    });

    it('findIntentsByContext throws error no response came from the DesktopAgent service, undefined message', async () => {
        const messageRouterClientMock = {
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

        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.findIntentsByContext({ type: "fdc3.Instrument" }))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('findIntentsByContext throws error as it got error message from the DesktopAgent service', async () => {
        const fdc3IntentResponse: Fdc3FindIntentsByContextResponse = {
            appIntents: [],
            error: "Error happens..."
        };
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(fdc3IntentResponse)}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.findIntentsByContext({ type: "testType" }))
            .rejects
            .toThrow(fdc3IntentResponse.error);

        expect(messageRouterClientMock.invoke).toHaveBeenCalledWith(ComposeUITopic.findIntentsByContext(), JSON.stringify({ fdc3InstanceId: window.composeui.fdc3.config?.instanceId, context: { type: "testType" } }));
    });

    it('findIntentsByContext resolves and calls the invoke method of the messageRouterClient', async () => {
        const request = {
            fdc3InstanceId: window?.composeui?.fdc3.config?.instanceId,
            context: {
                type: 'fdc3.Instrument'
            }
        };

        const response: Fdc3FindIntentsByContextResponse = {
            appIntents: [
                {
                    intent: { name: 'fdc3.Instrument', displayName: 'fdc3.Instrument' },
                    apps: [
                        { appId: "appId1" },
                        { appId: "appId2" }
                    ]
                },
                {
                    intent: { name: 'fdc3.Instrument2', displayName: 'fdc3.Instrument2' },
                    apps: [
                        { appId: "appId3" },
                        { appId: "appId4" }
                    ]
                }
            ]
        };

        const messageRouterClientMock = {
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
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        var resultAppIntent = await testDesktopAgent.findIntentsByContext({ type: "fdc3.Instrument" });

        expect(messageRouterClientMock.invoke).toHaveBeenCalledWith(ComposeUITopic.findIntentsByContext(), JSON.stringify(request));
        expect(resultAppIntent).toMatchObject(response.appIntents!);
    });

    it('raiseIntent will throw exception, due no answer came from the DesktopAgent service', async () => {
        const messageRouterClientMock = {
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

        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.raiseIntent("testIntent", { type: "fdc3.Instrument" }))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);

        expect(messageRouterClientMock.invoke).toHaveBeenCalledTimes(1);
    });

    it('raiseIntent will throw exception, due error was defined by the DesktopAgent service', async () => {
        const fdc3IntentResponse: Fdc3RaiseIntentResponse = {
            messageId: "1",
            error: "Error happens..."
        };
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(fdc3IntentResponse)}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.raiseIntent("testIntent", { type: "testType" }))
            .rejects
            .toThrow(fdc3IntentResponse.error);

        expect(messageRouterClientMock.invoke).toHaveBeenCalledTimes(1);
    });

    it('raiseIntent will resolve the first item, due the DesktopAgent service sent just one application', async () => {
        const fdc3IntentResponse: Fdc3RaiseIntentResponse = {
            messageId: "1",
            intent: "test",
            appMetadata: { appId: "test1" }
        };
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(fdc3IntentResponse)}`) })
        };


        const channelFactoryMock = {
            getChannel: jest.fn((channelId: string, channelType: ChannelType) => { return <Promise<Channel>>Promise.reject("not implemented") }),
            createPrivateChannel: jest.fn(() => { return <Promise<PrivateChannel>>Promise.reject("not implemented") }),
            getIntentListener: jest.fn((intent: string, handler: IntentHandler) => { return <Promise<Listener>>Promise.reject("not implemented") })
        };

        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        var result = await testDesktopAgent.raiseIntent("test", { type: "test" });
        expect(messageRouterClientMock.invoke).toHaveBeenCalledTimes(1);
        expect(result.intent).toMatch("test");
        expect(result.source).toMatchObject(<AppIdentifier>fdc3IntentResponse.appMetadata)

    });

    it('addIntentListener will resolve an intentListener', async () => {
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify({ stored: true })}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent('dummyPath', messageRouterClientMock);
        const intentHandler: IntentHandler = (context, metadata) => {
            return;
        };
        const result = await testDesktopAgent.addIntentListener("testIntent", intentHandler);

        expect(messageRouterClientMock.subscribe).toHaveBeenCalledTimes(1);
        expect(result).toBeInstanceOf(ComposeUIIntentListener);
    });

    it('addIntentListener will fail as no answer provided', async () => {
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve("") })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent('dummyPath', messageRouterClientMock);
        const intentHandler: IntentHandler = (context, metadata) => {
            return;
        };

        await expect(testDesktopAgent.addIntentListener("testIntent", intentHandler))
            .rejects
            .toThrow(ComposeUIErrors.NoAnswerWasProvided);
    });

    it('addIntentListener will fail as the service not stored', async () => {
        const messageRouterClientMock = {
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
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify({ stored: false, error: undefined })}`) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent('dummyPath', messageRouterClientMock);
        const intentHandler: IntentHandler = (context, metadata) => {
            return;
        };

        await expect(testDesktopAgent.addIntentListener("testIntent", intentHandler))
            .rejects
            .toThrow(ComposeUIErrors.SubscribeFailure);
    });
});