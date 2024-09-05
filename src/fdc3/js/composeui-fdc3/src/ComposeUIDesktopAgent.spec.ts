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
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { Channel, ChannelError, ContextHandler, DesktopAgent } from '@finos/fdc3';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { ChannelFactory } from './infrastructure/ChannelFactory';
import { ComposeUIPrivateChannel } from './infrastructure/ComposeUIPrivateChannel';
import { ChannelType } from './infrastructure/ChannelType';

const dummyContext = { type: "dummyContextType" };
const dummyChannelId = "dummyId";
let messageRouterClient: MessageRouter;
let desktopAgent: DesktopAgent;

const testInstrument = {
    type: 'fdc3.instrument',
    id: {
        ticker: 'AAPL'
    }
};
const contextMessageHandlerMock = jest.fn((something) => {
    return "dummy";
});

describe('Tests for ComposeUIDesktopAgent implementation API', () => {
    //Be aware that currently the tests are for User channels mostly!
    beforeEach(async () => {
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId : "test"
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
            invoke: jest.fn(() => {
                return Promise.resolve(JSON.stringify({ context: "", payload: `${JSON.stringify(dummyContext)}` }))
            })
        };

        let channelFactory: ChannelFactory = {
            createPrivateChannel: jest.fn(() => { return Promise.resolve(new ComposeUIPrivateChannel("privateId", messageRouterClient, true)) }),
            getChannel: jest.fn(async (channelId: string, channelType: ChannelType) => {
                if (channelId == dummyChannelId) { return new ComposeUIChannel(channelId, channelType, messageRouterClient); }
                else { throw new Error(ChannelError.NoChannelFound); }
            }),
            getIntentListener: jest.fn(() => Promise.reject("Not implemented")),
            createAppChannel: jest.fn(() => Promise.reject("Not implemented")),
            joinUserChannel: jest.fn(() => Promise.resolve(new ComposeUIChannel(dummyChannelId, "user", messageRouterClient))),
            getUserChannels: jest.fn(() => Promise.reject("Not implemented")),
            getContextListener: jest.fn((channel: Channel, handler: ContextHandler, contextType?: string) => {return Promise.resolve(new ComposeUIContextListener(messageRouterClient, handler, contextType))})
        };

        desktopAgent = new ComposeUIDesktopAgent(messageRouterClient, channelFactory);
        await desktopAgent.joinUserChannel(dummyChannelId);
        await new Promise(f => setTimeout(f, 100));
    });

    it('ComposeUIDesktopAgent could not be created as no instanceId found on window object', async () => {
        window.composeui.fdc3.config = undefined;
        expect(() => new ComposeUIDesktopAgent(messageRouterClient))
            .toThrowError(ComposeUIErrors.InstanceIdNotFound);
    });

    it('broadcast will trigger publish method of the messageRouter', async () => {
        await desktopAgent.broadcast(testInstrument);
        expect(messageRouterClient.publish).toBeCalledTimes(1);
        expect(messageRouterClient.publish).toHaveBeenCalledWith(ComposeUITopic.broadcast(dummyChannelId, "user"), JSON.stringify(testInstrument));
    });

    it('broadcast will fail as the current channel is not defined', async () => {
        await desktopAgent.leaveCurrentChannel();
        await expect(desktopAgent.broadcast(testInstrument))
            .rejects
            .toThrow("The current channel has not been set.");
    });

    it('default channel can be retrieved', async () => {
        var result = await desktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: "user" });
    });

    it('leaveCurrentChannel will set the current user channel to undefined', async () => {
        await desktopAgent.leaveCurrentChannel();
        var result = await desktopAgent.getCurrentChannel();
        expect(result).toBeFalsy();
    });

    it('getCurrentChannel will get the current user channel', async () => {
        await desktopAgent.leaveCurrentChannel();
        await desktopAgent.joinUserChannel(dummyChannelId);
        var result = await desktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: "user" });
    });

    it('leaveCurrentChannel will trigger the current channel listeners to unsubscribe', async () => {
        const listener = <ComposeUIContextListener>await desktopAgent.addContextListener("fdc3.instrument", contextMessageHandlerMock)

        await desktopAgent.leaveCurrentChannel();
        var result = await desktopAgent.getCurrentChannel();
        expect(result).toBeFalsy();
        expect(listener.handleContextMessage(dummyContext))
            .rejects
            .toThrow("The current listener is not subscribed.");
    });

    it('joinChannel will set the current user channel', async () => {
        await desktopAgent.joinChannel(dummyChannelId);
        var result = await desktopAgent.getCurrentChannel();
        expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: "user" });
    });

    it('createPrivateChannel returns the channel', async () => {
        let channel = await desktopAgent.createPrivateChannel();
        expect(channel).toBeInstanceOf(ComposeUIPrivateChannel);
        expect(channel.type).toBe("private");
    });
});