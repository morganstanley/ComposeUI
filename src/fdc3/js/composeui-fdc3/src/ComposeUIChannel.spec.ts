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
import { ComposeUIChannel } from './infrastructure/ComposeUIChannel';
import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { Channel, ChannelError, Context } from '@finos/fdc3';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';

const dummyChannelId = "dummyId";
let messageRouterClient: MessageRouter;
let testChannel: Channel;

const testInstrument = {
    type: 'fdc3.instrument',
    id: {
        ticker: 'AAPL'
    }
};
const contextMessageHandlerMock = jest.fn((_) => {
    return "dummy";
});

describe('Tests for ComposeUIChannel implementation API', () => {
    beforeEach(() => {
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
            invoke: jest.fn(() => Promise.resolve(""))
        };

        testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
    });

    it('broadcast will call messageRouters publish method', async () => {
        await testChannel.broadcast(testInstrument);
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
    });

    it('broadcast will set the lastContext to test instrument', async () => {
        await testChannel.broadcast(testInstrument);
        const resultContext = await testChannel.getCurrentContext();
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
        expect(resultContext).toMatchObject(testInstrument);
    });

    it('getCurrentContext will overwrite the lastContext of the same type', async () => {
        await testChannel.broadcast(testInstrument);
        const testInstrument2 = {
            type: 'fdc3.instrument',
            id: {
                ticker: 'SMSN'
            }
        };
        await testChannel.broadcast(testInstrument2);
        const resultContext = await testChannel.getCurrentContext();
        const resultContextWithContextType = await testChannel.getCurrentContext(testInstrument2.type);
        expect(messageRouterClient.publish).toBeCalledTimes(2);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument2));
        expect(resultContext).toMatchObject(testInstrument2);
        expect(resultContextWithContextType).toMatchObject<Partial<Context>>(testInstrument2);
    });

    it("getCurrentContext will return null as the given contextType couldn't be found in the saved contexts", async () => {
        const result = await testChannel.getCurrentContext("dummyContextType");
        expect(result).toBe(null);
    });

    // TODO: Test broadcast/getLastContext with different context and contextType combinations

    it('addContextListener will result a ComposeUIContextListener', async () => {
        await testChannel.broadcast(testInstrument);
        const resultListener = await testChannel.addContextListener('fdc3.instrument', contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIContextListener);
        expect(contextMessageHandlerMock).toHaveBeenCalledTimes(0); //as per the standard
    });

    // TODO: This doesn't test what it sais it tests
    it('addContextListener will treat contexType is ContextHandler as all types', async () => {
        const resultListener = await testChannel.addContextListener(null, contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIContextListener);
        expect(messageRouterClient.subscribe).toBeCalledTimes(1);
    });
});

describe("AppChanel tests", () => {

    beforeEach(() =>{
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
    
    it("getOrCreateChannel returns successfully after it found the AppChannel in the cache", async () => {
        const messageRouterClientMock = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({unsubscribe: () => {}});}),

            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify({ found: true })}`) })
        };

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);
        const channel1 = await desktopAgent.getOrCreateChannel("hello.world");
        const channel2 = await desktopAgent.getOrCreateChannel("hello.world");
        expect(channel2).toBe(channel1);
    });

    it("getOrCreateChannel creates a channel", async () => {
        let messageRouterClientMock: MessageRouter = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve<string | undefined>(undefined)})
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ found: false })))
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ success: true })))
        };
        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);
        const channel = await desktopAgent.getOrCreateChannel("hello.world");
        expect(channel).toBeInstanceOf(ComposeUIChannel);
    });

    it("getOrCreateChannel throws error as it received error from the DesktopAgent", async () => {
        let messageRouterClientMock: MessageRouter = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve<string | undefined>(undefined)})
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ found: false })))
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ success: false, error: "dummy" })))
        };

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);
        await expect(desktopAgent.getOrCreateChannel("hello.world"))
            .rejects
            .toThrow("dummy");
    });

    it("getOrCreateChannel throws error as it received no success without error message from the DesktopAgent", async () => {
        let messageRouterClientMock: MessageRouter = {
            clientId: "dummy",
            subscribe: jest.fn(() => { return Promise.resolve({unsubscribe: () => {}});}),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve<string | undefined>(undefined)})
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ found: false })))
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ success: false })))
        };

        const desktopAgent = new ComposeUIDesktopAgent(messageRouterClientMock);
        await expect(desktopAgent.getOrCreateChannel("hello.world"))
            .rejects
            .toThrow(ChannelError.CreationFailed);
    });
});
