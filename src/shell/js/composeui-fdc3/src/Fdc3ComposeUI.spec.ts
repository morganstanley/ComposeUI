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
import { Unsubscribable } from "rxjs";
import { ComposeUIChannel } from './ComposeUIChannel';
import { ComposeUIChannelType } from './ComposeUIChannelType';
import { EndpointDescriptor, InvokeOptions, MessageHandler, MessageRouter, PublishOptions, TopicMessage, TopicSubscriber } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIListener } from './ComposeUIListener';
import { ComposeUIDesktopAgent } from '.';

let messageRouterClient: MockMessageRouter;
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

    beforeEach(() => {
        messageRouterClient = new MockMessageRouter();
    });

    it('broadcast will call messageRouters publish method', async() => {
        const testChannel = new ComposeUIChannel("dummyTopic", ComposeUIChannelType.User, messageRouterClient);
        await testChannel.broadcast(testInstrument);
        expect(messageRouterClient.mock.publish).toHaveBeenCalled();
    });

    it('broadcast will set the lastContext to test instrument', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, ComposeUIChannelType.User, messageRouterClient);
        await testChannel.broadcast(testInstrument);
        const resultContext = await testChannel.getCurrentContext();
        const expectedObject = {
            Id: dummyTopic + "broadcast",
            Context: testInstrument
        };
        expect(messageRouterClient.mock.publish).toHaveBeenCalledTimes(1);
        expect(messageRouterClient.PublishedMessages.size).toBe(1);
        expect(messageRouterClient.PublishedMessages.get(dummyTopic + "broadcast")).toBe(JSON.stringify(expectedObject));
        expect(resultContext).toEqual(testInstrument);
    });

    it('getCurrentContext will result the lastContext', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, ComposeUIChannelType.User, messageRouterClient);
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
        expect(messageRouterClient.mock.publish).toBeCalledTimes(2);
        expect(resultContext).toBe(resultContextWithContextType);
        expect(resultContext).toBe(testInstrument2);
    });

    it('addContextListener will result a ComposeUIListener', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, ComposeUIChannelType.User, messageRouterClient);
        const resultListener = await testChannel.addContextListener('fdc3.instrument', instrument => {
            console.log(instrument);
        });
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
    });

    it('addContextListener will fail as per no contexTypet will be set', async() => {
        const testChannel = new ComposeUIChannel(dummyTopic, ComposeUIChannelType.User, messageRouterClient);
        await expect(testChannel.addContextListener(null, instrument => {}))
            .rejects
            .toEqual(new Error("addContextListener without contextType is depracted, please use the newer version."));
    });
});

describe('Tests for ComposeUIListener implementation API', () => {

    beforeEach(() => {
        messageRouterClient = new MockMessageRouter();
    });

    it('subscribe will call messagerouter subscribe method', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, instrument => { console.log(instrument); }, "dummyChannelId/dummyPath/", "fdc3.instrument");
        await testListener.subscribe("dummyBroadcast");
        expect(messageRouterClient.mock.subscribe).toHaveBeenCalled();
        expect(messageRouterClient.Subscribers.size).toBe(1);
        expect(messageRouterClient.Subscribers.has("dummyChannelId/dummyPath/dummyBroadcast")).toBeTruthy();
    });

    it('handleContextMessage will trigger the handler', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await testListener.subscribe("dummyChannelTopicSuffix");
        await testListener.handleContextMessage(testInstrument);
        expect(contextMessageHandlerMock).toHaveBeenCalledWith(testInstrument);
    });

    it('handleContextMessage will be rejected with Error as no handler', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        await expect(testListener.handleContextMessage(testInstrument))
            .rejects
            .toEqual(new Error("The current listener is not subscribed."));
    });

    it('unsubscribe will be true', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, "dummyChannelId/dummyPath/", "fdc3.instrument");
        await testListener.subscribe("dummyChannelId");
        const resultUnsubscription = testListener.unsubscribe();
        expect(resultUnsubscription).toBeTruthy();
    });

    it('unsubscribe will be false', async() => {
        const testListener = new ComposeUIListener(messageRouterClient, contextMessageHandlerMock, undefined, "fdc3.instrument");
        const resultUnsubscription = testListener.unsubscribe();
        expect(resultUnsubscription).toBeFalsy();
    });
});

describe('Tests for ComposeUIDesktopAgent implementation API', () => {
    //Be aware that currently the tests are for User channels mostly!
    beforeEach(() => {
        messageRouterClient = new MockMessageRouter();
    });

    it('broadcast will trigger publish method of the messageRouter', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await testDesktopAgent.broadcast(testInstrument);
        expect(messageRouterClient.mock.publish).toBeCalledTimes(1);
    });

    it('broadcast will fail as per the current channel is not defined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.broadcast(testInstrument))
            .rejects
            .toEqual(new Error("The current channel have not been set."));
    });

    it('addContextListener will trigger messageRouter subscribe method', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        const resultListener = await testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock);
        expect(resultListener).toBeInstanceOf(ComposeUIListener);
        expect(messageRouterClient.mock.subscribe).toBeCalledTimes(1);
    });

    it('addContextListener will fail as per the current channel is not defined', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock))
            .rejects
            .toEqual(new Error("The current channel is null or undefined"));
        expect(messageRouterClient.mock.subscribe).toBeCalledTimes(0);
    });

    it('addContextListener will fail as per the type of the context type is a function', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        await expect(testDesktopAgent.addContextListener(contextMessageHandlerMock))
            .rejects
            .toEqual(new Error("The contextType was type of ContextHandler, which would use a deprecated function, please use string or null for contextType!"));
        expect(messageRouterClient.mock.subscribe).toBeCalledTimes(0);
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
        expect(result).toEqual(new ComposeUIChannel("composeui/fdc3/v2.0/userchannels/dummyPath/", ComposeUIChannelType.User, messageRouterClient));
    });

    it('joinUserChannel will fail as per the channelId is not found', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.joinUserChannel("dummyPath2"))
            .rejects
            .toEqual(new Error("Channel couldn't be found in user channels"));
    });

    it('getOrCreateChannel will create a new APP channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        const result = await testDesktopAgent.getOrCreateChannel("dummyPath2");
        expect(result).toEqual(new ComposeUIChannel("composeui/fdc3/v2.0/userchannels/dummyPath2/", ComposeUIChannelType.App, messageRouterClient));
    });

    it('getCurrentChannel will get the current user channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinUserChannel("dummyPath");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toEqual(new ComposeUIChannel("composeui/fdc3/v2.0/userchannels/dummyPath/", ComposeUIChannelType.User, messageRouterClient));
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
        expect(result).toEqual(new ComposeUIChannel("composeui/fdc3/v2.0/userchannels/dummyPath/", ComposeUIChannelType.User, messageRouterClient));
    });

    it('joinChannel will set the current app channel', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.getOrCreateChannel("dummyPath2")
        await testDesktopAgent.joinChannel("dummyPath2");
        var result = await testDesktopAgent.getCurrentChannel();
        expect(result).toEqual(new ComposeUIChannel("composeui/fdc3/v2.0/userchannels/dummyPath2/", ComposeUIChannelType.App, messageRouterClient));
    });

    it('joinChannel will fail as per the channelId is not found', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await expect(testDesktopAgent.joinChannel("dummyNewNewId"))
            .rejects
            .toEqual(new Error("No channel is found with id: dummyNewNewId"));
    });

    it('joinChannel will fail as per the current channel is not null', async() => {
        const testDesktopAgent = new ComposeUIDesktopAgent("dummyPath", messageRouterClient);
        await testDesktopAgent.joinChannel("dummyPath");
        await expect(testDesktopAgent.joinChannel("dummyNewNewId"))
            .rejects
            .toEqual(new Error("The current channel is already instantiated."));
    });
});

//dummy mock implemetation to check if the subscribe/publish method will be called by the fdc3 implementation
class MockMessageRouter implements MessageRouter{

    public Subscribers: Map<string, number> = new Map<string, number>();
    public PublishedMessages: Map<string, string> = new Map<string, string>();

    constructor() {
        this.connect = jest.fn(this.connect);
        this.publish = jest.fn(this.publish);
        this.subscribe = jest.fn(this.subscribe);
        this.invoke = jest.fn(this.invoke);
        this.registerService = jest.fn(this.registerService);
        this.unregisterService = jest.fn(this.unregisterService);
        this.registerEndpoint = jest.fn(this.registerEndpoint);
        this.unregisterEndpoint = jest.fn(this.unregisterEndpoint);
    }

    connect(): Promise<void> {
        return Promise.resolve();
    }

    async subscribe(topic: string, subscriber: TopicSubscriber | ((message: TopicMessage) => void)): Promise<Unsubscribable> {
        if(this.Subscribers.has(topic)){
            let kvp = this.Subscribers.get(topic);
            if(kvp === undefined){
                kvp = 1;
            } else {
                kvp += 1;
            }
        } else {
            this.Subscribers.set(topic, 1);
        }
        return {
            unsubscribe: () => {
                this.Subscribers.delete(topic);
            }
        };
    }

    publish(topic: string, payload?: string, options?: PublishOptions): Promise<void> {
        return new Promise((resolve, reject) => {
            if(payload === null || payload === undefined){
                reject();
            }
            this.PublishedMessages.set(topic, payload!);
            resolve();
        });
    }

    invoke(endpoint: string, payload?: string, options?: InvokeOptions): Promise<string | undefined> {
        return Promise.resolve<string>("dummyReturnValue");
    }

    registerService(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void> {
        return Promise.resolve();
    }

    unregisterService(endpoint: string): Promise<void> {
        return Promise.resolve();
    }

    registerEndpoint(endpoint: string, handler: MessageHandler, descriptor?: EndpointDescriptor): Promise<void> {
        return Promise.resolve();
    }

    unregisterEndpoint(endpoint: string): Promise<void> {
        return Promise.resolve();
    }
    
    get mock(): jest.MockedObject<MessageRouter> {
        return jest.mocked(this);
    }
}