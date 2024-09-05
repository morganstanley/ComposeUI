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

import { ChannelError } from "@finos/fdc3";
import { ChannelFactory } from "./infrastructure/ChannelFactory";
import { MessageRouterChannelFactory } from "./infrastructure/MessageRouterChannelFactory";
import { Fdc3JoinUserChannelResponse } from "./infrastructure/messages/Fdc3JoinUserChannelResponse";
import { ComposeUIChannel } from "./infrastructure/ComposeUIChannel";
import { Fdc3GetUserChannelsResponse } from "./infrastructure/messages/Fdc3GetUserChannelsResponse";

describe("", () => {
    let channelFactory: ChannelFactory;

    it('joinUserChannel returns CreationFailed error as no response received', async() => {
        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(undefined)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        await expect(channelFactory.joinUserChannel("dummyId"))
            .rejects
            .toThrow(ChannelError.CreationFailed);

        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('joinUserChannel returns error as error received in the response', async() => {
        const response: Fdc3JoinUserChannelResponse = {
            error: "testError",
            success: false
        };

        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        await expect(channelFactory.joinUserChannel("dummyId"))
            .rejects
            .toThrow("testError");
        
        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('joinUserChannel returns error as the success property set to false', async() => {
        const response: Fdc3JoinUserChannelResponse = {
            displayMetadata: {
                name: "test",
            },
            success: false
        };

        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        await expect(channelFactory.joinUserChannel("dummyId"))
            .rejects
            .toThrow(ChannelError.CreationFailed);

        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('joinUserChannel returns channel', async() => {
        const response: Fdc3JoinUserChannelResponse = {
            displayMetadata: {
                name: "test",
            },
            success: true
        };

        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        const result = await channelFactory.joinUserChannel("dummyId");
        expect(result).toBeInstanceOf(ComposeUIChannel);
        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('getUserChannels returns NoChannelFound error as no response received', async() => {
        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(undefined)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        await expect(channelFactory.getUserChannels())
            .rejects
            .toThrow(ChannelError.NoChannelFound);

        expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('getUserChannels returns error as error response received', async() => {
        const response: Fdc3GetUserChannelsResponse = {
            error: "testError"
        };

        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        await expect(channelFactory.getUserChannels())
            .rejects
            .toThrow("testError");
            
            expect(messageRouterClient.invoke).toHaveBeenCalledTimes(1);
    });

    it('getUserChannels returns channels', async() => {
        const response: Fdc3GetUserChannelsResponse = {
            channels: [{id: "testId", type: "user", displayMetadata: {name: "testDisplayMetadata"}}]
        };

        let messageRouterClient = {
            clientId: "dummyId",
            subscribe: jest.fn(() => { return Promise.resolve({ unsubscribe: () => { } }); }),
            publish: jest.fn(() => { return Promise.resolve() }),
            connect: jest.fn(() => { return Promise.resolve() }),
            registerEndpoint: jest.fn(() => { return Promise.resolve() }),
            unregisterEndpoint: jest.fn(() => { return Promise.resolve() }),
            registerService: jest.fn(() => { return Promise.resolve() }),
            unregisterService: jest.fn(() => { return Promise.resolve() }),
            invoke: jest.fn(() => { return Promise.resolve(`${JSON.stringify(response)}`)})
        };

        channelFactory = new MessageRouterChannelFactory(messageRouterClient, "dummyId");

        const result = await channelFactory.getUserChannels();
        expect(result).toBeDefined();
        expect(result.length).toBe(1);
        expect(result.at(0)).toMatchObject({id: "testId", type: "user", displayMetadata: {name: "testDisplayMetadata"}})
    });
});