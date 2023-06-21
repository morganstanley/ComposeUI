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
import { ComposeUIDesktopAgent } from '.';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { Channel, ChannelError, Context, Listener } from '@finos/fdc3';
import { Fdc3ChannelMessage } from './infrastructure/messages/Fdc3ChannelMessage';
import { randomUUID } from 'crypto';
import { Fdc3GetCurrentContextMessage } from './infrastructure/messages/Fdc3GetCurrentContextMessage';

const dummyContext = {type: "dummyContextType"};
const dummyChannelId = "dummyId";
let messageRouterClient: MessageRouter = {
    subscribe: jest.fn(() => {
        return Promise.resolve({unsubscribe: () => {}});}),
        
    publish: jest.fn(() => { return Promise.resolve() }),
    connect: jest.fn(() => { return Promise.resolve() }),
    registerEndpoint: jest.fn(() => { return Promise.resolve() }),
    unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
    registerService: jest.fn(() => { return Promise.resolve() }),
    unregisterService: jest.fn(() => { return Promise.resolve() }),
    invoke: jest.fn(() => { return Promise.resolve(JSON.stringify({context: "", payload: `${JSON.stringify(new Fdc3ChannelMessage(dummyChannelId, dummyContext))}` })) })
};

const testInstrument = {
    type: 'fdc3.instrument',
    id: {
        ticker: 'AAPL'
    }
};
const dummyTopic= "dummyTopic";
const contextMessageHandlerMock = jest.fn((something) => {
    return "dummy";
});

describe('Tests for ComposeUIChannel implementation API', () => {    

    it('broadcast will call messageRouters publish method', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyTopic), JSON.stringify(new Fdc3ChannelMessage("dummyTopic", testInstrument)));
    });

    it('broadcast will set the lastContext to test instrument', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const resultContext = await testChannel.getCurrentContext();
        expect(messageRouterClient.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.invoke).toHaveBeenCalledWith(ComposeUITopic.getCurrentContext(dummyTopic, testChannel.type), JSON.stringify(new Fdc3GetCurrentContextMessage(null)));
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyTopic), JSON.stringify(new Fdc3ChannelMessage(dummyTopic, testInstrument)));
        expect(resultContext).toMatchObject<Partial<Context>>({Id: dummyChannelId, Context: dummyContext});
    });

    it('getCurrentContext will result the lastContext', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
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
        expect(messageRouterClient.invoke).toBeCalledTimes(2);
        expect(messageRouterClient.publish).toBeCalledTimes(2);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyTopic), JSON.stringify(new Fdc3ChannelMessage(dummyTopic, testInstrument)));
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyTopic), JSON.stringify(new Fdc3ChannelMessage(dummyTopic, testInstrument2)));
        expect(messageRouterClient.invoke).toHaveBeenCalledWith(ComposeUITopic.getCurrentContext(dummyTopic, testChannel.type), JSON.stringify(new Fdc3GetCurrentContextMessage(null)));
        expect(messageRouterClient.invoke).toHaveBeenCalledWith(ComposeUITopic.getCurrentContext(dummyTopic, testChannel.type), JSON.stringify(new Fdc3GetCurrentContextMessage(testInstrument2.type)));
        expect(resultContext).toMatchObject<Partial<Context>>({Id: dummyChannelId, Context: dummyContext});
        expect(resultContextWithContextType).toMatchObject<Partial<Context>>(testInstrument2);
    });

    it('getCurrentContext will return null as per the given contextType couldnt be found in the saved contexts', async() =>{
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
        const result = await testChannel.getCurrentContext("dummyContextType");
        expect(result).toBe(null);
    });

    it('addContextListener will result a ComposeUIListener', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const resultListener = await testChannel.addContextListener('fdc3.instrument', contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(contextMessageHandlerMock).toHaveBeenCalledTimes(0); //as per the standard
    });

    it('addContextListener will fail as per contexType is ContextHandler', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, "user", messageRouterClient);
        await expect(testChannel.addContextListener(test => {}))
            .rejects
            .toThrow("addContextListener with contextType as ContextHandler is deprecated, please use the newer version.");
    });
});

describe('Tests for ComposeUIListener implementation API', () => {

    it('subscribe will call messagerouter subscribe method', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, instrument => { console.log(instrument); }, "dummyChannelId", "fdc3.instrument");
        await testListener.subscribe();
        expect(messageRouterClient.subscribe).toHaveBeenCalledTimes(1);
        //expect(messageRouterClient.subscribe).toHaveBeenCalledWith(ComposeUITopic.broadcast("dummyChannelId"), jest.fn());
    });

    it('handleContextMessage will trigger the handler', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await testListener.subscribe();
        await testListener.handleContextMessage(testInstrument);
        expect(contextMessageHandlerMock).toHaveBeenCalledWith(testInstrument);
    });

    it('handleContextMessage will resolve the LatestContext saved for ComposeUIListener', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await testListener.subscribe();
        testListener.LatestContext = testInstrument;
        await testListener.handleContextMessage();
        expect(contextMessageHandlerMock).toHaveBeenCalledWith(testListener.LatestContext);
    });

    it('handleContextMessage will resolve an empty context', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await testListener.subscribe();
        await testListener.handleContextMessage();
        expect(contextMessageHandlerMock).toHaveBeenCalledWith({type: ""});
    });

    it('handleContextMessage will be rejected with Error as no handler', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await expect(testListener.handleContextMessage(testInstrument))
            .rejects
            .toThrow("The current listener is not subscribed.");
    });

    it('unsubscribe will be true', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, "dummyChannelId", "fdc3.instrument");
        await testListener.subscribe();
        const resultUnsubscription = await testListener.unsubscribe();
        expect(resultUnsubscription).toBeTruthy();
    });

    it('unsubscribe will be false', async() => {
        const testListener = new ComposeUIListener(randomUUID(), messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        const resultUnsubscription = await testListener.unsubscribe();
        expect(resultUnsubscription).toBeFalsy();
    });
});

describe('Tests for ComposeUIDesktopAgent implementation API', () => {
    //Be aware that currently the tests are for User channels mostly!

    it('broadcast will trigger publish method of the messageRouter', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent(dummyTopic, messageRouterClient);
        await testDesktopAgent.joinUserChannel(dummyTopic);
        await testDesktopAgent.broadcast(testInstrument);
        expect(messageRouterClient.publish).toBeCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyTopic), JSON.stringify(new Fdc3ChannelMessage(dummyTopic, testInstrument)));
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
        //expect(messageRouterClient.subscribe).toHaveBeenCalledWith({Id: "dummyPath", Context: {type: fdc3.instrument", id: { ticker: "AAPL"} }});
    });

    it('addContextListener will fail as per the current channel is not defined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock))
            .rejects
            .toThrow("The current channel have not been set.");
        expect(messageRouterClient.subscribe).toBeCalledTimes(0);
    });

    it('addContextListener will fail as per the type of the context type is a function', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await expect(testDesktopAgent.addContextListener(contextMessageHandlerMock))
            .rejects
            .toThrow("The contextType was type of ContextHandler, which would use a deprecated function, please use string or null for contextType!");
        expect(messageRouterClient.subscribe).toBeCalledTimes(0);
    });

    it('getUserChannels will return the created userchannels', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        var result = await testDesktopAgent.getUserChannels();
        expect(result.length).toBe(1);
    });

    it('joinUserChannel will set the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath", type: "user"});
    });

    it('joinUserChannel will fail as per the channelId is not found', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.joinUserChannel("dummyPath2"))
            .rejects
            .toThrow(ChannelError.NoChannelFound);
    });

    it('getOrCreateChannel will create a new APP channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        const result = await testDesktopAgent.getOrCreateChannel("dummyPath2");
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath2", type: "app" });
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
        expect(result).toBe(undefined);
    });

    it('getInfo will provide information of ComposeUI', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        var result = await testDesktopAgent.getInfo();
        expect(result.fdc3Version).toBe("2.0.0");
        expect(result.provider).toBe("ComposeUI");
    });

    it('joinChannel will set the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath", type: "user" });
    });

    it('joinChannel will set the current app channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.getOrCreateChannel("dummyPath2")
        await testDesktopAgent.joinChannel("dummyPath2");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: "dummyPath2", type: "app" });
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