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
import { ComposeUIListener } from './infrastructure/ComposeUIListener';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { Channel, ChannelError, Context } from '@finos/fdc3';
import { Fdc3GetCurrentContextRequest } from './infrastructure/messages/Fdc3GetCurrentContextRequest';
import { Fdc3FindChannelRequest } from './infrastructure/messages/Fdc3FindChannelRequest';

const dummyContext = {type: "dummyContextType"};
const dummyChannelId = "dummyId";
let messageRouterClient: MessageRouter;

const testInstrument = {
    type: 'fdc3.instrument',
    id: {
        ticker: 'AAPL'
    }
};
const contextMessageHandlerMock = jest.fn((something) => {
    return "dummy";
});

describe('Tests for ComposeUIChannel implementation API', () => {    

    beforeEach(() => {
        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({unsubscribe: () => {}});}),
                
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(JSON.stringify(dummyContext))})
        }
    });

    it('broadcast will call messageRouters publish method', async() => {
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId,"user"), JSON.stringify(testInstrument));
    });

    it('broadcast will set the lastContext to test instrument', async() => {
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const resultContext = await testChannel.retrieveCurrentContext();
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);        
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
        expect(resultContext).toMatchObject(testInstrument);
    });

    it('getCurrentContext will result the lastContext', async() => {
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const testInstrument2 = {
            type: 'fdc3.instrument',
            id: {
                ticker: 'SMSN'
            }
        };
        await testChannel.broadcast(testInstrument2);
        const resultContext = await testChannel.retrieveCurrentContext();
        const resultContextWithContextType = await testChannel.retrieveCurrentContext(testInstrument2.type);
        expect(messageRouterClient.publish).toBeCalledTimes(2);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument2));        
        expect(resultContext).toMatchObject(testInstrument2);
        expect(resultContextWithContextType).toMatchObject<Partial<Context>>(testInstrument2);
    });

    it('getCurrentContext will return null as per the given contextType couldnt be found in the saved contexts', async() =>{
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        const result = await testChannel.retrieveCurrentContext("dummyContextType");
        expect(result).toBe(null);
    });

    it('addContextListener will result a ComposeUIListener', async() => {
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const resultListener = await testChannel.addContextListener('fdc3.instrument', contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(contextMessageHandlerMock).toHaveBeenCalledTimes(0); //as per the standard
    });

    it('addContextListener will treat contexType is ContextHandler as all types', async() => {
        const testChannel = new ComposeUIChannel(dummyChannelId, "user", messageRouterClient);
        const resultListener = await testChannel.addContextListener(test => {});
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(messageRouterClient.subscribe).toBeCalledTimes(1);        
    });
});

describe('Tests for ComposeUIListener implementation API', () => {
    beforeEach(() => {
        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({unsubscribe: () => {}});}),
                
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(JSON.stringify({context: "", payload: `${JSON.stringify(dummyContext)}` })) })
        }
    });

    it('subscribe will call messagerouter subscribe method', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, instrument => { console.log(instrument); }, "dummyChannelId", "user", "fdc3.instrument");
        await testListener.subscribe();
        expect(messageRouterClient.subscribe).toHaveBeenCalledTimes(1);
    });

    it('handleContextMessage will trigger the handler', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "", "user", "fdc3.instrument");
        await testListener.subscribe();
        await testListener.handleContextMessage(testInstrument);
        expect(contextMessageHandlerMock).toHaveBeenCalledWith(testInstrument);
    });

    it('handleContextMessage will resolve the LatestContext saved for ComposeUIListener', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "", "user", "fdc3.instrument");
        await testListener.subscribe();
        testListener.latestContext = testInstrument;
        await testListener.handleContextMessage();
        expect(contextMessageHandlerMock).toHaveBeenCalledWith(testListener.latestContext);
    });

    it('handleContextMessage will resolve an empty context', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "", "user", "fdc3.instrument");
        await testListener.subscribe();
        await testListener.handleContextMessage();
        expect(contextMessageHandlerMock).toHaveBeenCalledWith({type: ""});
    });

    it('handleContextMessage will be rejected with Error as no handler', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "", "user", "fdc3.instrument");
        await expect(testListener.handleContextMessage(testInstrument))
            .rejects
            .toThrow("The current listener is not subscribed.");
    });

    it('unsubscribe will be true', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "dummyChannelId", "user", "fdc3.instrument");
        await testListener.subscribe();
        const resultUnsubscription = testListener.unsubscribe();
        expect(resultUnsubscription).toBeTruthy();
    });

    it('unsubscribe will be false', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "", "user", "fdc3.instrument");
        const resultUnsubscription = testListener.unsubscribe();
        expect(resultUnsubscription).toBeFalsy();
    });
});

describe('Tests for ComposeUIDesktopAgent implementation API', () => {
    //Be aware that currently the tests are for User channels mostly!
    beforeEach(() => {
        messageRouterClient = {
            clientId: "dummy",
            subscribe: jest.fn(() => {
                return Promise.resolve({unsubscribe: () => {}});}),
                
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(JSON.stringify({context: "", payload: `${JSON.stringify(dummyContext)}` })) })
        }
    });

    it('broadcast will trigger publish method of the messageRouter', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent(dummyChannelId, messageRouterClient);
        await testDesktopAgent.joinUserChannel(dummyChannelId);
        await testDesktopAgent.broadcast(testInstrument);
        expect(messageRouterClient.publish).toBeCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
    });

    it('broadcast will fail as per the current channel is not defined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.broadcast(testInstrument))
            .rejects
            .toThrow("The current channel have not been set.");
    });

    it('addContextListener will trigger messageRouter subscribe method', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await testDesktopAgent.broadcast(testInstrument); //this will set the last context
        const resultListener = await testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(messageRouterClient.subscribe).toBeCalledTimes(1);
    });

    it('addContextListener will fail as per the current channel is not defined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock))
            .rejects
            .toThrow("The current channel have not been set.");
        expect(messageRouterClient.subscribe).toBeCalledTimes(0);
    });

    it('addContextListener will treat function context type as all types', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
       var resultListener = await testDesktopAgent.addContextListener(contextMessageHandlerMock)
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(messageRouterClient.subscribe).toBeCalledTimes(1);
    });

    it('getUserChannels will return the created userchannels', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        var result = await testDesktopAgent.getUserChannels();
        expect(result.length).toBe(1);
    });

    it('joinUserChannel will fail as per the current channel is already instantiated', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath"); //Joining first
        await expect(testDesktopAgent.joinUserChannel("dummyPath"))
            .rejects
            .toThrow(new Error(ChannelError.AccessDenied));
    });

    it('joinUserChannel will invoke the invoke method of the messageRouter', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath2");
        expect(messageRouterClient.invoke).toHaveBeenCalledWith(ComposeUITopic.joinUserChannel(), JSON.stringify(new Fdc3FindChannelRequest("dummyPath2", "user")));
    });

    it('joinUserChannel will throw the error from the response of the invoke method', async() => {
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
            invoke: jest.fn(() => { return Promise.resolve(JSON.stringify({context: "", payload: `${JSON.stringify({error: "Error happens..."})}` })) })
        };
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClientMock);
        await expect(testDesktopAgent.joinUserChannel("dummyPath2"))
            .rejects
            .toThrow(new Error("Error happens..."));
        expect(messageRouterClientMock.invoke).toHaveBeenCalledWith(ComposeUITopic.joinUserChannel(), JSON.stringify(new Fdc3FindChannelRequest("dummyPath2", "user")));
    });

    it('joinUserChannel will set the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath", type: "user"});
    });

    it('getCurrentChannel will get the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath", type: "user" });
    });

    it('leaveCurrentChannel will set the current user channel to undefined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await testDesktopAgent.leaveCurrentChannel();
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toBeFalsy();
    });

    it('leaveCurrentChannel will trigger the current channel listeners to unsubscribe', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock);
        await testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock);
        await expect(testDesktopAgent.leaveCurrentChannel())
            .resolves
            .not
            .toThrow();
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toBeFalsy();
    });

    it('leaveCurrentChannel will fail as per the listener is already unsubscribed', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        const listener = await testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock);
        listener.unsubscribe();

        await expect(testDesktopAgent.leaveCurrentChannel())
            .rejects
            .toThrow(new Error(`Listener couldn't unsubscribe. IsSubscribed: false, Listener: ${listener}`));

        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toBeFalsy();
    });

    it('getInfo will provide information of ComposeUI', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        var result = await testDesktopAgent.getInfo();
        expect(result.fdc3Version).toBe("2.0");
        expect(result.provider).toBe("ComposeUI");
    });

    it('joinChannel will set the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath", type: "user" });
    });

    it('joinChannel will fail as per the channelId is not found', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.joinChannel("dummyNewNewId"))
            .rejects
            .toThrow("No channel is found with id: dummyNewNewId");
    });

    it('joinChannel will fail as per the current channel is not null', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinChannel("dummyPath");
        await expect(testDesktopAgent.joinChannel("dummyNewNewId"))
            .rejects
            .toThrow(ChannelError.CreationFailed);
    });
});