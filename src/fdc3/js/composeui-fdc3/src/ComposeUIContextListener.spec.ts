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

import { ContextHandler } from '@finos/fdc3';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { Fdc3AddContextListenerResponse } from './infrastructure/messages/Fdc3AddContextListenerResponse';
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';
import { describe, it, expect, beforeEach, vi } from 'vitest';

const dummyContext = { type: "dummyContextType" };
const dummyChannelId = "dummyId";
let jsonMessagingMock: JsonMessaging;
let subscribeSpy: any;
let handlerSpy: any;
let testListener: ComposeUIContextListener;

const testInstrument = {
    type: 'fdc3.instrument',
    id: {
        ticker: 'AAPL'
    }
};

const wrongContext = {
    type: 'dummy'
}

export interface ContextHandlerMock {
    contextHandler: ContextHandler;
}

let contextMessageHandlerMock : ContextHandlerMock;

describe('Tests for ComposeUIContextListener implementation API', () => {
    
    beforeEach(async () => {

        contextMessageHandlerMock = {
            contextHandler: (_: unknown) => {}
        };

        // @ts-ignore
        window.composeui = {
            fdc3: {
                config: {
                    appId: "testAppId",
                    instanceId: "testInstanceId"
                },
                channelId : "test",
                openAppIdentifier: {
                    openedAppContextId: "test"
                }
            },
            messaging: {
                communicator: undefined
            }
        };

        const response: Fdc3AddContextListenerResponse = {
            success: true,
            id: "testListenerId"
        };

        const messagingMock : IMessaging = {
            subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
            publish: vi.fn(() => Promise.resolve()),
            registerService: vi.fn(() => Promise.resolve({
                unsubscribe: () => {},
                [Symbol.asyncDispose]: () => Promise.resolve()
            })),
            invokeService: vi
                .fn(() => { return Promise.resolve(`${JSON.stringify(undefined)}`) })
                .mockImplementationOnce(() => Promise.resolve(`${JSON.stringify(response)}`))
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify({ context: "", payload: `${JSON.stringify(dummyContext)}` })))
        };

        jsonMessagingMock = new JsonMessaging(messagingMock);
        subscribeSpy = vi.spyOn(jsonMessagingMock, 'subscribeJson');
        handlerSpy = vi.spyOn(contextMessageHandlerMock, 'contextHandler');

        testListener = new ComposeUIContextListener(true, jsonMessagingMock, contextMessageHandlerMock.contextHandler, "fdc3.instrument");
        await testListener.subscribe(dummyChannelId, "user");
    });

    it('subscribe will call messaging subscribe method', async () => {
        expect(subscribeSpy).toHaveBeenCalledTimes(1);
    });

    it('handleContextMessage will trigger the handler', async () => {
        await testListener.handleContextMessage(testInstrument);
        expect(handlerSpy).toHaveBeenCalledWith(testInstrument);
    });

    it('handleContextMessage will be rejected with Error if unsubscribed', async () => {
        testListener = new ComposeUIContextListener(true, jsonMessagingMock, contextMessageHandlerMock.contextHandler, "fdc3.instrument");
        testListener.unsubscribe();
        await expect(testListener.handleContextMessage(testInstrument))
            .rejects
            .toThrow("The current listener is not subscribed.");
    });

    it('handleContextMessage will be rejected with Error if called with wrong context type', async () => {
        await expect(testListener.handleContextMessage(wrongContext))
            .rejects
            .toThrow(Error);
    });

    it('handleContextMessage wont call the handler as the context from the open call was not handled yet', async() => {
        testListener.setOpenHandled(false);
        await testListener.handleContextMessage(testInstrument);
        expect(handlerSpy).toHaveBeenCalledTimes(0);
    });
});