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
import { OpenError } from '@finos/fdc3';
import { MessagingOpenClient } from './infrastructure/MessagingOpenClient';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { Fdc3OpenResponse } from './infrastructure/messages/Fdc3OpenResponse';
import { Fdc3GetOpenedAppContextResponse } from './infrastructure/messages/Fdc3GetOpenedAppContextResponse';
import { OpenAppIdentifier } from './infrastructure/OpenAppIdentifier';
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';

describe('MessagingOpenClient tests', () => {
  let client: MessagingOpenClient;
  let jsonMessaging: JsonMessaging;
  const openAppIdentifier: OpenAppIdentifier = { openedAppContextId: 'contextId' };

  const baseMessagingMock = (): IMessaging => ({
    subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
    publish: vi.fn(() => Promise.resolve()),
    registerService: vi.fn(() =>
      Promise.resolve({
        unsubscribe: () => {},
        [Symbol.asyncDispose]: () => Promise.resolve()
      })
    ),
    invokeService: vi.fn(() => Promise.resolve(null))
  });

  beforeEach(() => {
    // @ts-ignore
    window.composeui = {
      fdc3: {
        config: { appId: 'testAppId', instanceId: 'testInstanceId' },
        channelId: 'test',
        openAppIdentifier: { openedAppContextId: 'contextId' }
      }
    };
    const messagingMock = baseMessagingMock();
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
  });

  it('open throws AppNotFound when no AppIdentifier provided', async () => {
    await expect(client.open()).rejects.toThrow(OpenError.AppNotFound);
  });

  it('open throws NoAnswerWasProvided when backend returns undefined', async () => {
    await expect(client.open('appId1')).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
  });

  it('open throws backend error from error response', async () => {
    const response: Fdc3OpenResponse = { error: 'testError' };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify(response))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    await expect(client.open('appId1')).rejects.toThrow('testError');
  });

  it('open returns AppIdentifier on success', async () => {
    const response: Fdc3OpenResponse = {
      appIdentifier: { appId: 'appId1', instanceId: 'instanceId1' }
    };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify(response))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    const result = await client.open({ appId: 'appId1' }, { type: 'fdc3.instrument' });
    expect(result).toMatchObject({ appId: 'appId1', instanceId: 'instanceId1' });
  });

  it('getOpenedAppContext throws when context id not set', async () => {
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging);
    await expect(client.getOpenedAppContext()).rejects.toThrow(
      'Context id is not defined on the window object.'
    );
  });

  it('getOpenedAppContext throws NoAnswerWasProvided on undefined response', async () => {
    await expect(client.getOpenedAppContext()).rejects.toThrow(
      ComposeUIErrors.NoAnswerWasProvided
    );
  });

  it('getOpenedAppContext throws NoAnswerWasProvided on wrong payload shape', async () => {
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify({ payload: 'wrongPayload' }))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    await expect(client.getOpenedAppContext()).rejects.toThrow(
      ComposeUIErrors.NoAnswerWasProvided
    );
  });

  it('getOpenedAppContext throws backend error', async () => {
    const response: Fdc3GetOpenedAppContextResponse = { error: 'testGetOpenedAppContextError' };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify(response))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    await expect(client.getOpenedAppContext()).rejects.toThrow('testGetOpenedAppContextError');
  });

  it('getOpenedAppContext throws NoAnswerWasProvided when context missing', async () => {
    const response: Fdc3GetOpenedAppContextResponse = {};
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify(response))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    await expect(client.getOpenedAppContext()).rejects.toThrow(
      ComposeUIErrors.NoAnswerWasProvided
    );
  });

  it('getOpenedAppContext returns context', async () => {
    const response: Fdc3GetOpenedAppContextResponse = { context: { type: 'fdc3.instrument' } };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify(response))
    );
    jsonMessaging = new JsonMessaging(messagingMock);
    client = new MessagingOpenClient('fdc3InstanceId', jsonMessaging, openAppIdentifier);
    const result = await client.getOpenedAppContext();
    expect(result.type).toBe('fdc3.instrument');
  });
});
