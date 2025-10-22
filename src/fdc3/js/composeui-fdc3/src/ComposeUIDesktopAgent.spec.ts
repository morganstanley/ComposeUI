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
import { IMessaging, JsonMessaging } from '@morgan-stanley/composeui-messaging-abstractions';
import { ComposeUIChannel } from './infrastructure/ComposeUIChannel';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { ComposeUIDesktopAgent } from './ComposeUIDesktopAgent';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { Channel, ChannelError, ContextHandler } from '@finos/fdc3';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { ChannelFactory } from './infrastructure/ChannelFactory';
import { ComposeUIPrivateChannel } from './infrastructure/ComposeUIPrivateChannel';
import { ChannelType } from './infrastructure/ChannelType';
import { Fdc3GetOpenedAppContextResponse } from './infrastructure/messages/Fdc3GetOpenedAppContextResponse';

const dummyContext = { type: 'dummyContextType' };
const dummyChannelId = 'dummy';
let jsonMessagingMock: JsonMessaging;
let messagingMock: IMessaging;
let desktopAgent: ComposeUIDesktopAgent;

const testInstrument = {
  type: 'fdc3.instrument',
  id: { ticker: 'AAPL' }
};

const contextMessageHandlerMock = vi.fn((_ctx) => 'dummy');

const buildChannelFactory = (jm: JsonMessaging): ChannelFactory => ({
  createPrivateChannel: vi.fn(() =>
    Promise.resolve(new ComposeUIPrivateChannel('privateId', 'localInstance', jm, true))
  ),
  getChannel: vi.fn(async (channelId: string, channelType: ChannelType) => {
    if (channelId === dummyChannelId) return new ComposeUIChannel(channelId, channelType, jm);
    throw new Error(ChannelError.NoChannelFound);
  }),
  getIntentListener: vi.fn(() => Promise.reject('Not implemented')),
  createAppChannel: vi.fn(() => Promise.reject('Not implemented')),
  joinUserChannel: vi.fn(() => Promise.resolve(new ComposeUIChannel(dummyChannelId, 'user', jm))),
  getUserChannels: vi.fn(() => Promise.reject('Not implemented')),
  getContextListener: vi.fn(
    (_openHandled: boolean, _channel: Channel, handler: ContextHandler, contextType?: string) =>
      Promise.resolve(new ComposeUIContextListener(true, jm, handler, contextType))
  )
});

describe('Tests for ComposeUIDesktopAgent implementation API', () => {
  beforeEach(async () => {
    // @ts-ignore
    window.composeui = {
      fdc3: {
        config: { appId: 'testAppId', instanceId: 'testInstanceId' },
        channelId: 'test',
        openAppIdentifier: { openedAppContextId: 'test' }
      }
    };

    messagingMock = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() =>
        Promise.resolve({
          unsubscribe: () => {},
          [Symbol.asyncDispose]: () => Promise.resolve()
        })
      ),
      invokeService: vi.fn(() =>
        Promise.resolve(JSON.stringify({ context: '', payload: `${JSON.stringify(dummyContext)}` }))
      )
    };

    jsonMessagingMock = new JsonMessaging(messagingMock);
    const channelFactory = buildChannelFactory(jsonMessagingMock);
    desktopAgent = new ComposeUIDesktopAgent(jsonMessagingMock, channelFactory);
    await desktopAgent.joinUserChannel(dummyChannelId);
  });

  it('ComposeUIDesktopAgent throws when no instanceId', () => {
    // @ts-ignore
    window.composeui.fdc3.config = undefined;
    expect(() => new ComposeUIDesktopAgent(jsonMessagingMock)).toThrowError(
      ComposeUIErrors.InstanceIdNotFound
    );
  });

  it('broadcast publishes context', async () => {
    const publishSpy = vi.spyOn(jsonMessagingMock, 'publishJson');
    await desktopAgent.broadcast(testInstrument);
    expect(publishSpy).toHaveBeenCalledTimes(1);
    expect(publishSpy).toHaveBeenCalledWith(
      ComposeUITopic.broadcast(dummyChannelId, 'user'),
      testInstrument
    );
  });

  it('broadcast fails without current channel', async () => {
    await desktopAgent.leaveCurrentChannel();
    await expect(desktopAgent.broadcast(testInstrument)).rejects.toThrow(
      'The current channel has not been set.'
    );
  });

  it('default current channel retrieved', async () => {
    const result = await desktopAgent.getCurrentChannel();
    expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: 'user' });
  });

  it('leaveCurrentChannel clears channel', async () => {
    await desktopAgent.leaveCurrentChannel();
    const result = await desktopAgent.getCurrentChannel();
    expect(result).toBeFalsy();
  });

  it('getCurrentChannel after rejoin', async () => {
    await desktopAgent.leaveCurrentChannel();
    await desktopAgent.joinUserChannel(dummyChannelId);
    const result = await desktopAgent.getCurrentChannel();
    expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: 'user' });
  });

  it('context listener cannot handle when not subscribed', async () => {
    await desktopAgent.leaveCurrentChannel();
    const listener = (await desktopAgent.addContextListener(
      'fdc3.instrument',
      contextMessageHandlerMock
    )) as ComposeUIContextListener;
    const current = await desktopAgent.getCurrentChannel();
    expect(current).toBeFalsy();
    await expect(listener.handleContextMessage(dummyContext)).rejects.toThrow(
      'The current listener is not subscribed.'
    );
  });

  it('joinChannel sets current user channel', async () => {
    await desktopAgent.joinChannel(dummyChannelId);
    const result = await desktopAgent.getCurrentChannel();
    expect(result).toMatchObject<Partial<Channel>>({ id: dummyChannelId, type: 'user' });
  });

  it('createPrivateChannel returns private channel', async () => {
    const channel = await desktopAgent.createPrivateChannel();
    expect(channel).toBeInstanceOf(ComposeUIPrivateChannel);
    expect(channel.type).toBe('private');
  });

  it('addContextListener handles openedAppContext on subscribe', async () => {
    const openedResp: Fdc3GetOpenedAppContextResponse = {
      context: { type: 'fdc3.instrument' }
    };

    const messagingMock2: IMessaging = {
      subscribe: vi.fn(() => Promise.resolve({ unsubscribe: () => {} })),
      publish: vi.fn(() => Promise.resolve()),
      registerService: vi.fn(() =>
        Promise.resolve({
          unsubscribe: () => {},
          [Symbol.asyncDispose]: () => Promise.resolve()
        })
      ),
      invokeService: vi
        .fn(() => Promise.resolve(`${JSON.stringify(undefined)}`))
        .mockImplementationOnce(() => Promise.resolve(`${JSON.stringify(openedResp)}`))
    };
    const jsonMessaging2 = new JsonMessaging(messagingMock2);
    const channelFactory2 = buildChannelFactory(jsonMessaging2);
    desktopAgent = new ComposeUIDesktopAgent(jsonMessaging2, channelFactory2);

    await desktopAgent.getOpenedAppContext();
    const currentChannel = await desktopAgent.getCurrentChannel();
    expect(currentChannel).toBe(null);

    const listener = await desktopAgent.addContextListener(
      'fdc3.instrument',
      contextMessageHandlerMock
    );
    expect(listener).toBeInstanceOf(ComposeUIContextListener);
    expect(contextMessageHandlerMock).toHaveBeenCalledTimes(1);
  });
});