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
import { IMessaging, JsonMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { ChannelHandler } from './infrastructure/ChannelHandler';
import { MessagingChannelHandler } from './infrastructure/MessagingChannelHandler';
import { MessagingIntentsClient } from './infrastructure/MessagingIntentsClient';
import { IntentsClient } from './infrastructure/IntentsClient';
import { MetadataClient } from './infrastructure/MetadataClient';
import { MessagingMetadataClient } from './infrastructure/MessagingMetadataClient';
import { OpenClient } from "./infrastructure/OpenClient";
import { MessagingOpenClient } from "./infrastructure/MessagingOpenClient";

export class ComposeUIDesktopAgent implements DesktopAgent, AsyncDisposable {
    private currentChannel?: Channel;
    private topLevelContextListeners: ComposeUIContextListener[] = [];
    private intentListeners: Listener[] = [];
    private channelHandler: ChannelHandler;
    private intentsClient: IntentsClient;
    private metadataClient: MetadataClient;
    private openClient: OpenClient;
    private openedAppContext?: Context;
    private openedAppContextHandled: boolean = false;

    //TODO: we should enable passing multiple channelId to the ctor.
    constructor(
        messaging: IMessaging, 
        channelHandler?: ChannelHandler,
        intentsClient?: IntentsClient,
        metadataClient?: MetadataClient,
        openClient?: OpenClient) {

        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) {
            throw new Error(ComposeUIErrors.InstanceIdNotFound);
        }

        const jsonMessaging: JsonMessaging = new JsonMessaging(messaging);

        // TODO: inject this directly instead of the messageRouter
        this.channelHandler = channelHandler ?? new MessagingChannelHandler(jsonMessaging, window.composeui.fdc3.config.instanceId);
        this.intentsClient = intentsClient ?? new MessagingIntentsClient(jsonMessaging, this.channelHandler);
        this.metadataClient = metadataClient ?? new MessagingMetadataClient(jsonMessaging, window.composeui.fdc3.config);
        this.openClient = openClient ?? new MessagingOpenClient(window.composeui.fdc3.config.instanceId!, jsonMessaging, window.composeui.fdc3.openAppIdentifier);
    }

    public async [Symbol.asyncDispose](): Promise<void> {
        console.debug("Disposing ComposeUIDesktopAgent");
        
        await this.channelHandler[Symbol.asyncDispose]();

        for (const listener of this.intentListeners) {
            await listener.unsubscribe();
        }

        this.intentListeners = [];

        for (const listener of this.topLevelContextListeners) {
            await listener.unsubscribe();
        }

        this.topLevelContextListeners = [];
    }

    // This regiters an endpoint to listen when an action was initiated from the UI to select a user channel to join to.
    public async init(): Promise<void> {
        await this.channelHandler.configureChannelSelectorFromUI();
    }

    public async open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        return await this.openClient.open(app, context);
    }

    public async findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        return await this.intentsClient.findIntent(intent, context, resultType);
    }

    public async findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        return await this.intentsClient.findIntentsByContext(context, resultType);
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
        return await this.intentsClient.raiseIntent(intent, context, app);
    }

    public async raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        return await this.intentsClient.raiseIntentForContext(context, app);
    }

    public async addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        var listener = await this.channelHandler.getIntentListener(intent, handler);
        this.intentListeners.push(listener);
        return listener;
    }

    public async addContextListener(contextType?: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        if (contextType && typeof contextType != 'string') {
            handler = contextType;
            contextType = null;
        }

        if (this.openedAppContext 
            && handler 
            && (contextType == this.openedAppContext?.type || this.openedAppContext.type == null || !this.openedAppContext.type)) {                
                
                if (this.openedAppContextHandled === false) {
                    handler(this.openedAppContext);
                    this.openedAppContextHandled = true;
                }
        } 
        
        if (!this.openedAppContext && this.openedAppContextHandled !== true) {
            //There is no context to handle -aka app was not opened via the fdc3.open
            this.openedAppContextHandled = true;
        }

        const listener = <ComposeUIContextListener>await this.channelHandler.getContextListener(this.openedAppContextHandled, this.currentChannel, handler, contextType);
        this.topLevelContextListeners.push(listener);

        if (!this.currentChannel) {
            return listener;
        }

        return await new Promise<Listener>((resolve) => {
            resolve(listener);
        }).finally(() => {
            queueMicrotask(async () => await this.callHandlerOnChannelsCurrentContext(listener));
        });
    }

    public async getUserChannels(): Promise<Array<Channel>> {
        return await this.channelHandler.getUserChannels();
    }

    public async joinUserChannel(channelId: string): Promise<void> {
        if (this.currentChannel) {
            //DesktopAgnet clients can listen on only one channel
            console.debug("Leaving current channel: ", this.currentChannel.id);
            await this.leaveCurrentChannel();
        }

        let channel = await this.channelHandler.joinUserChannel(channelId);

        console.debug("Joined to user channel: ", channelId);

        if (!channel) {
            throw new Error(ChannelError.NoChannelFound);
        }

        this.currentChannel = channel;

        for (const listener of this.topLevelContextListeners) {
            await listener.subscribe(this.currentChannel.id, this.currentChannel.type)
                .finally(() => {
                    queueMicrotask(async () => await this.callHandlerOnChannelsCurrentContext(listener));
                });
        }
    }

    public async getOrCreateChannel(channelId: string): Promise<Channel> {
        let appChannel = await this.channelHandler.createAppChannel(channelId);
        return appChannel!;
    }

    public async createPrivateChannel(): Promise<PrivateChannel> {
        return await this.channelHandler.createPrivateChannel();
    }

    public async getCurrentChannel(): Promise<Channel | null> {
        return this.currentChannel ?? null;
    }

    public async leaveCurrentChannel(): Promise<void> {
        //The context listeners, that have been added through the `fdc3.addContextListener()` should unsubscribe
        console.debug("Unsubscribing top level context listeners: ", this.topLevelContextListeners);
        for (const listener of this.topLevelContextListeners) {
            await listener.unsubscribe();
        }
        
        if (this.currentChannel) {
            await this.channelHandler.leaveCurrentChannel();
            this.currentChannel = undefined;
        }
    }

    public async getInfo(): Promise<ImplementationMetadata> {
        return await this.metadataClient.getInfo();
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

    public async getOpenedAppContext(): Promise<void> {
        try {
            this.openedAppContext = await this.openClient.getOpenedAppContext();
        } catch (err) {
            console.error("The opened app via fdc3.open() could not retrieve the context: ", err);
        }
    }

    private async callHandlerOnChannelsCurrentContext(listener: ComposeUIContextListener) : Promise<void> {
        const lastContext = await this.currentChannel!.getCurrentContext(listener.contextType);

        if (lastContext) {
            await listener.handleContextMessage(lastContext);
        }
    }
}
