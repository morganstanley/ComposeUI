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

import {
    AppIdentifier,
    AppIntent,
    AppMetadata,
    Channel,
    ChannelError,
    Context,
    ContextHandler,
    DesktopAgent,
    ImplementationMetadata,
    IntentHandler,
    IntentResolution,
    Listener,
    PrivateChannel
} from '@finos/fdc3';

import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { ChannelFactory } from './infrastructure/ChannelFactory';
import { MessageRouterChannelFactory } from './infrastructure/MessageRouterChannelFactory';
import { MessageRouterIntentsClient } from './infrastructure/MessageRouterIntentsClient';
import { IntentsClient } from './infrastructure/IntentsClient';
import { MetadataClient } from './infrastructure/MetadataClient';
import { MessageRouterMetadataClient } from './infrastructure/MessageRouterMetadataClient';

declare global {
    interface Window {
        composeui: {
            fdc3: {
                config: AppIdentifier | undefined;
                channelId : string;
            }
        }
        fdc3: DesktopAgent;
    }
}

export class ComposeUIDesktopAgent implements DesktopAgent {
    private appChannels: Channel[] = [];
    private userChannels: Channel[] = [];
    private privateChannels: Channel[] = [];
    private currentChannel?: Channel;
    private currentChannelListeners: ComposeUIContextListener[] = [];
    private intentListeners: Listener[] = [];
    private channelFactory: ChannelFactory;
    private intentsClient: IntentsClient;
    private metadataClient: MetadataClient;

    //TODO: we should enable passing multiple channelId to the ctor.
    constructor(channelId: string, messageRouterClient: MessageRouter, channelFactory?: ChannelFactory) {
        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) {
            throw new Error(ComposeUIErrors.InstanceIdNotFound);
        }

        // TODO: inject this directly instead of the messageRouter
        this.channelFactory = channelFactory ?? new MessageRouterChannelFactory(messageRouterClient, window.composeui.fdc3.config.instanceId);
        this.intentsClient = new MessageRouterIntentsClient(messageRouterClient, this.channelFactory);
        this.metadataClient = new MessageRouterMetadataClient(messageRouterClient, window.composeui.fdc3.config);

        setTimeout(
            async () => {
                window.fdc3 = this;
                window.dispatchEvent(new Event("fdc3Ready"));
            }, 0);
    }

    //TODO
    public open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        throw new Error("Not implemented");
    }

    public findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        return this.intentsClient.findIntent(intent, context, resultType);
    }

    public findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        return this.intentsClient.findIntentsByContext(context, resultType);
    }

    public async findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        return await this.metadataClient.findInstances(app);
    }

    public async broadcast(context: Context): Promise<void> {
        if (!this.currentChannel) {
            throw new Error(ComposeUIErrors.CurrentChannelNotSet);
        }

        return this.currentChannel.broadcast(context);
    }

    public async raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        return this.intentsClient.raiseIntent(intent, context, app);
    }

    //TODO
    public raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    public async addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        var listener = await this.channelFactory.getIntentListener(intent, handler);
        this.intentListeners.push(listener);
        return listener;
    }

    public async addContextListener(contextType?: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        if (!this.currentChannel) {
            throw new Error(ComposeUIErrors.CurrentChannelNotSet);
        }

        if (contextType && typeof contextType != 'string') {
            handler = contextType;
            contextType = null;
        }

        const listener = <ComposeUIContextListener>await this.currentChannel!.addContextListener(contextType ?? null, handler!);

        const lastContext = await this.currentChannel!.getCurrentContext(contextType ?? undefined)

        if (lastContext) {
            //TODO: timing issue
            setTimeout(async() => await listener.handleContextMessage(lastContext), 100);
        }

        this.currentChannelListeners.push(listener);
        return listener;
    }

    public async getUserChannels(): Promise<Array<Channel>> {
        return await this.channelFactory.getUserChannels();
    }

    public async joinUserChannel(channelId: string): Promise<void> {
        if (this.currentChannel) {
            return;
        }

        let channel = this.userChannels.find(innerChannel => innerChannel.id == channelId);
        if (!channel) {
            channel = await this.channelFactory.joinUserChannel(channelId);
        }

        if (!channel) {
            throw new Error(ChannelError.NoChannelFound);
        }

        this.addChannel(channel);
        this.currentChannel = channel;
    }

    public async getOrCreateChannel(channelId: string): Promise<Channel> {
        let appChannel = this.appChannels.find(channel => channel.id == channelId);
        if (appChannel) {
            return appChannel;
        }

        try {
            appChannel = await this.channelFactory.getChannel(channelId, "app");
        } catch (err) {
            if (!appChannel) {
                appChannel = await this.channelFactory.createAppChannel(channelId);            
            }
        }

        this.addChannel(appChannel!);
        return appChannel!;
    }

    //TODO
    public async createPrivateChannel(): Promise<PrivateChannel> {
        return this.channelFactory.createPrivateChannel();
    }

    public async getCurrentChannel(): Promise<Channel | null> {
        return this.currentChannel ?? null;
    }

    //TODO: add messageRouter message that we are leaving the current channel to notify the backend.
    public async leaveCurrentChannel(): Promise<void> {
        this.currentChannel = undefined;
        this.currentChannelListeners.forEach(listener => {
            listener.unsubscribe();
        });
        this.currentChannelListeners = [];
    }

    public async getInfo(): Promise<ImplementationMetadata> {
        return this.metadataClient.getInfo();
    }

    public async getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        return await this.metadataClient.getAppMetadata(app);
    }

    // Deprecated, alias to getUserChannels
    // https://fdc3.finos.org/docs/2.0/api/ref/DesktopAgent#getsystemchannels-deprecated
    public getSystemChannels(): Promise<Channel[]> {
        return this.getUserChannels();
    }

    // Deprecated, alias to joinUserChannel
    // https://fdc3.finos.org/docs/2.0/api/ref/DesktopAgent#joinchannel-deprecated
    public joinChannel(channelId: string): Promise<void> {
        return this.joinUserChannel(channelId);
    }

    private addChannel(channel: Channel): void {
        if (channel == null) return;
        switch (channel.type) {
            case "app":
                this.appChannels.push(channel);
                break;
            case "user":
                this.userChannels.push(channel);
                break;
            case "private":
                this.privateChannels.push(channel);
                break;
        }
    }
}