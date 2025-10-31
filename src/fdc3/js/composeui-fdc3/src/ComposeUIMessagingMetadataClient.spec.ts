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
import { ImplementationMetadata } from '@finos/fdc3';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { Fdc3GetInfoResponse } from './infrastructure/messages/Fdc3GetInfoResponse';
import { Fdc3FindInstancesResponse } from './infrastructure/messages/Fdc3FindInstancesResponse';
import { Fdc3GetAppMetadataResponse } from './infrastructure/messages/Fdc3GetAppMetadataResponse';
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';

describe('MessagingMetadataClient tests', () => {
  beforeEach(() => {
    // @ts-ignore
    window.composeui = {
      fdc3: {
        config: { appId: 'testAppId', instanceId: 'testInstanceId' },
        channelId: 'test',
        openAppIdentifier: { openedAppContextId: 'test' }
      }
    };
  });

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

  it('getInfo throws NoAnswerWasProvided', async () => {
    const messagingMock = baseMessagingMock();
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.getInfo()).rejects.toThrow(ComposeUIErrors.NoAnswerWasProvided);
  });

  it('getInfo throws backend error', async () => {
    const response = { error: 'dummyError' };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.getInfo()).rejects.toThrow('dummyError');
  });

  it('getInfo returns ImplementationMetadata', async () => {
    const implementationMetadata: ImplementationMetadata = {
      fdc3Version: '2.0.0',
      provider: 'ComposeUI',
      providerVersion: '1.0.0',
      optionalFeatures: {
        OriginatingAppMetadata: false,
        UserChannelMembershipAPIs: true
      },
      appMetadata: { appId: 'dummyAppId' }
    };
    const response: Fdc3GetInfoResponse = { implementationMetadata };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    const result = await agent.getInfo();
    expect(result).toMatchObject(implementationMetadata);
  });

  it('findInstances throws NoAnswerWasProvided', async () => {
    const messagingMock = baseMessagingMock();
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.findInstances({ appId: 'test' })).rejects.toThrow(
      ComposeUIErrors.NoAnswerWasProvided
    );
  });

  it('findInstances throws backend error', async () => {
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify({ error: 'dummyError' }))
    );
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.findInstances({ appId: 'test' })).rejects.toThrow('dummyError');
  });

  it('findInstances returns instances', async () => {
    const response: Fdc3FindInstancesResponse = {
      instances: [{ appId: 'test', instanceId: 'id' }]
    };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    const result = await agent.findInstances({ appId: 'test' });
    expect(result).toMatchObject([{ appId: 'test', instanceId: 'id' }]);
  });

  it('getAppMetadata throws NoAnswerWasProvided', async () => {
    const messagingMock = baseMessagingMock();
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.getAppMetadata({ appId: 'test' })).rejects.toThrow(
      ComposeUIErrors.NoAnswerWasProvided
    );
  });

  it('getAppMetadata throws backend error', async () => {
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() =>
      Promise.resolve(JSON.stringify({ error: 'dummyError' }))
    );
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    await expect(agent.getAppMetadata({ appId: 'test' })).rejects.toThrow('dummyError');
  });

  it('getAppMetadata returns appMetadata', async () => {
    const response: Fdc3GetAppMetadataResponse = { appMetadata: { appId: 'test' } };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const agent = new ComposeUIDesktopAgent(new JsonMessaging(messagingMock));
    const result = await agent.getAppMetadata({ appId: 'test' });
    expect(result).toMatchObject({ appId: 'test' });
  });
});
