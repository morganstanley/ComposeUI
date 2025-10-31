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

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { AppIdentifier, IntentHandler } from '@finos/fdc3';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { Fdc3FindIntentsByContextResponse } from './infrastructure/messages/Fdc3FindIntentsByContextResponse';
import { Fdc3RaiseIntentResponse } from './infrastructure/messages/Fdc3RaiseIntentResponse';
import { ComposeUIIntentListener } from './infrastructure/ComposeUIIntentListener';
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';

describe("Tests for ComposeUIDesktopAgent's intent handling", () => {
  beforeEach(() => {
    // @ts-ignore
    window.composeui = {
      fdc3: {
        config: {
          appId: 'testAppId',
          instanceId: 'testInstanceId'
        },
        channelId: 'test',
        openAppIdentifier: {
          openedAppContextId: 'test'
        }
      }
    };
  });

  it('findIntent throws when no answer provided', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(null))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    await expect(agent.findIntent('testIntent')).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
  });

  it('findIntent throws backend error', async () => {
    const fdc3IntentResponse = { error: 'Error happens...' };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve<null | string>(null))
                .mockImplementationOnce(() => Promise.resolve(JSON.stringify(fdc3IntentResponse)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);

    const agent = new ComposeUIDesktopAgent(jsonMessaging);

    await expect(agent.findIntent('testIntent')).rejects.toThrow('Error happens...');
  });

  it('findIntent returns AppIntent', async () => {
    const payload = { appIntent: { itent: 'dummyIntent', apps: [{ appId: 'appId1' }, { appId: 'appdId2' }] } };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify(payload)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const result = await agent.findIntent('dummyIntent');

    expect(result).toMatchObject(payload.appIntent);
  });

  it('findIntentsByContext throws when undefined response', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(null))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    await expect(agent.findIntentsByContext({ type: 'fdc3.Instrument' })).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
  });

  it('findIntentsByContext throws backend error', async () => {
    const response: Fdc3FindIntentsByContextResponse = { appIntents: [], error: 'Error happens...' };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify(response)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    await expect(agent.findIntentsByContext({ type: 'testType' })).rejects.toThrow(response.error);
  });

  it('findIntentsByContext returns appIntents', async () => {
    const request = {
      fdc3InstanceId: 'testInstanceId',
      context: { type: 'fdc3.Instrument' }
    };
    const response: Fdc3FindIntentsByContextResponse = {
      appIntents: [
        { intent: { name: 'fdc3.Instrument', displayName: 'fdc3.Instrument' }, apps: [{ appId: 'appId1' }, { appId: 'appId2' }] },
        { intent: { name: 'fdc3.Instrument2', displayName: 'fdc3.Instrument2' }, apps: [{ appId: 'appId3' }, { appId: 'appId4' }] }
      ]
    };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify(response)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const result = await agent.findIntentsByContext({ type: 'fdc3.Instrument' });
    expect(result).toMatchObject(response.appIntents!);
  });

  it('raiseIntent throws when no answer', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(null))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    await expect(agent.raiseIntent('testIntent', { type: 'fdc3.Instrument' })).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('raiseIntent throws backend error', async () => {
    const intentResp: Fdc3RaiseIntentResponse = { messageId: '1', error: 'Error happens...' };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify(intentResp)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    await expect(agent.raiseIntent('testIntent', { type: 'testType' })).rejects.toThrow(intentResp.error);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('raiseIntent resolves with appMetadata', async () => {
    const intentResp: Fdc3RaiseIntentResponse = {
      messageId: '1',
      intent: 'test',
      appMetadata: { appId: 'test1' }
    };
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify(intentResp)))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const result = await agent.raiseIntent('test', { type: 'test' });
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
    expect(result.intent).toBe('test');
    expect(result.source).toMatchObject(<AppIdentifier>intentResp.appMetadata);
  });

  it('addIntentListener returns ComposeUIIntentListener', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify({ stored: true })))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const handler: IntentHandler = () => {};
    const listener = await agent.addIntentListener('testIntent', handler);
    expect(messagingMock.subscribe).toHaveBeenCalledTimes(1);
    expect(listener).toBeInstanceOf(ComposeUIIntentListener);
  });

  it('addIntentListener fails no answer', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(null))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const handler: IntentHandler = () => {};
    await expect(agent.addIntentListener('testIntent', handler)).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
  });

  it('addIntentListener fails stored false', async () => {
    const messagingMock: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() => Promise.resolve({ unsubscribe: () => {}, [Symbol.asyncDispose]: () => Promise.resolve() })),
      invokeService: vi.fn(() => Promise.resolve(JSON.stringify({ stored: false, error: undefined })))
    };
    const jsonMessaging = new JsonMessaging(messagingMock);
    const agent = new ComposeUIDesktopAgent(jsonMessaging);
    const handler: IntentHandler = () => {};
    await expect(agent.addIntentListener('testIntent', handler)).rejects.toThrow(ComposeUIErrors.SubscribeFailure);
  });
});
