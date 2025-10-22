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

import { describe, it, expect, vi } from 'vitest';
import { ChannelError } from '@finos/fdc3';
import { MessagingChannelFactory } from './infrastructure/MessagingChannelFactory';
import { Fdc3JoinUserChannelResponse } from './infrastructure/messages/Fdc3JoinUserChannelResponse';
import { Fdc3GetUserChannelsResponse } from './infrastructure/messages/Fdc3GetUserChannelsResponse';
import { ComposeUIChannel } from './infrastructure/ComposeUIChannel';
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';

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

describe('MessagingChannelFactory tests', () => {
  it('joinUserChannel rejects CreationFailed when no response', async () => {
    const messagingMock = baseMessagingMock();
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    await expect(factory.joinUserChannel('dummyId')).rejects.toThrow(ChannelError.CreationFailed);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('joinUserChannel rejects backend error', async () => {
    const response: Fdc3JoinUserChannelResponse = { error: 'testError', success: false };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    await expect(factory.joinUserChannel('dummyId')).rejects.toThrow('testError');
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('joinUserChannel rejects CreationFailed when success false without error', async () => {
    const response: Fdc3JoinUserChannelResponse = {
      displayMetadata: { name: 'test' },
      success: false
    };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    await expect(factory.joinUserChannel('dummyId')).rejects.toThrow(ChannelError.CreationFailed);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('joinUserChannel resolves channel on success', async () => {
    const response: Fdc3JoinUserChannelResponse = {
      displayMetadata: { name: 'test' },
      success: true
    };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    const result = await factory.joinUserChannel('dummyId');
    expect(result).toBeInstanceOf(ComposeUIChannel);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('getUserChannels rejects NoChannelFound when no response', async () => {
    const messagingMock = baseMessagingMock();
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    await expect(factory.getUserChannels()).rejects.toThrow(ChannelError.NoChannelFound);
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('getUserChannels rejects backend error', async () => {
    const response: Fdc3GetUserChannelsResponse = { error: 'testError' };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    await expect(factory.getUserChannels()).rejects.toThrow('testError');
    expect(messagingMock.invokeService).toHaveBeenCalledTimes(1);
  });

  it('getUserChannels returns channels array', async () => {
    const response: Fdc3GetUserChannelsResponse = {
      channels: [{ id: 'testId', type: 'user', displayMetadata: { name: 'testDisplayMetadata' } }]
    };
    const messagingMock = baseMessagingMock();
    messagingMock.invokeService = vi.fn(() => Promise.resolve(JSON.stringify(response)));
    const jsonMessaging = new JsonMessaging(messagingMock);
    const factory = new MessagingChannelFactory(jsonMessaging, 'localInstance');
    const result = await factory.getUserChannels();
    expect(result).toBeDefined();
    expect(result.length).toBe(1);
    expect(result.at(0)).toMatchObject({
      id: 'testId',
      type: 'user',
      displayMetadata: { name: 'testDisplayMetadata' }
    });
  });
});
