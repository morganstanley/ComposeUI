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
    PrivateChannel,
    ResolveError
} from '@finos/fdc3';

import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIChannel } from './infrastructure/ComposeUIChannel';
import { ChannelType } from './infrastructure/ChannelType';
import { ComposeUIContextListener } from './infrastructure/ComposeUIContextListener';
import { Fdc3FindChannelRequest } from './infrastructure/messages/Fdc3FindChannelRequest';
import { Fdc3FindChannelResponse } from './infrastructure/messages/Fdc3FindChannelResponse';
import { ComposeUITopic } from './infrastructure/ComposeUITopic';
import { ComposeUIIntentListener } from './infrastructure/ComposeUIIntentListener';
import { Fdc3RaiseIntentRequest } from './infrastructure/messages/Fdc3RaiseIntentRequest';
import { ComposeUIIntentResolution } from './infrastructure/ComposeUIIntentResolution';
import { Fdc3RaiseIntentResponse } from './infrastructure/messages/Fdc3RaiseIntentResponse';
import { Fdc3FindIntentRequest } from './infrastructure/messages/Fdc3FindIntentRequest';
import { Fdc3FindIntentResponse } from './infrastructure/messages/Fdc3FindIntentResponse';
import { Fdc3FindIntentsByContextRequest } from './infrastructure/messages/Fdc3FindIntentsByContextRequest';
import { Fdc3FindIntentsByContextResponse } from './infrastructure/messages/Fdc3FindIntentsByContextResponse';
import { ComposeUIErrors } from './infrastructure/ComposeUIErrors';
import { Fdc3IntentListenerRequest } from './infrastructure/messages/Fdc3IntentListenerRequest';
import { Fdc3IntentListenerResponse } from './infrastructure/messages/Fdc3IntentListenerResponse';

declare global {
    interface Window {
        composeui: {
            fdc3: {
                config: AppIdentifier | undefined;
            }
        }
        fdc3: DesktopAgent;
    }
}

export class ComposeUIDesktopAgent implements DesktopAgent {
    private appChannels: ComposeUIChannel[] = [];
    private userChannels: ComposeUIChannel[] = [];
    private privateChannels: ComposeUIChannel[] = [];
    private currentChannel?: ComposeUIChannel;
    private messageRouterClient: MessageRouter;
    private currentChannelListeners: ComposeUIContextListener[] = [];
    private intentListeners: ComposeUIIntentListener[] = [];

    //TODO: we should enable passing multiple channelId to the ctor.
    constructor(channelId: string, messageRouterClient: MessageRouter) {
        this.messageRouterClient = messageRouterClient;
        const channel = new ComposeUIChannel(
            channelId,
            "user",
            this.messageRouterClient);
        this.addChannel(channel);
        if (!window.composeui.fdc3.config || !window.composeui.fdc3.config.instanceId) throw new Error(ComposeUIErrors.InstanceIdNotFound);
        setTimeout(
            async () => {
                await this.joinUserChannel(channelId);
                window.fdc3 = this;
                window.dispatchEvent(new Event("fdc3Ready"));
            }, 0);
    }

    //TODO
    public open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        throw new Error("Not implemented");
    }

    public findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        return new Promise(async (resolve, reject) => {
            const request = new Fdc3FindIntentRequest(window.composeui.fdc3.config!.instanceId!, intent, context, resultType);
            const message = await this.messageRouterClient.invoke(ComposeUITopic.findIntent(), JSON.stringify(request));
            if (!message) {
                return reject(new Error(ComposeUIErrors.NoAnswerWasProvided));
            }

            const findIntentResponse = <Fdc3FindIntentResponse>JSON.parse(message);
            if (findIntentResponse.error) {
                return reject(new Error(findIntentResponse.error));
            }
            else {
                return resolve(findIntentResponse.appIntent!);
            }
        });
    }

    public findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        return new Promise(async (resolve, reject) => {
            const request = new Fdc3FindIntentsByContextRequest(window.composeui.fdc3.config!.instanceId!, context, resultType);
            const message = await this.messageRouterClient.invoke(ComposeUITopic.findIntentsByContext(), JSON.stringify(request));
            if (!message) {
                return reject(new Error(ComposeUIErrors.NoAnswerWasProvided));
            }

            const findIntentsByContextResponse = <Fdc3FindIntentsByContextResponse>JSON.parse(message);
            if (findIntentsByContextResponse.error) {
                return reject(new Error(findIntentsByContextResponse.error));
            }

            return resolve(findIntentsByContextResponse.appIntents!);
        });
    }

    //TODO
    public findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        throw new Error("Not implemented");
    }

    public broadcast(context: Context): Promise<void> {
        return new Promise((resolve, reject) => {
            if (!this.currentChannel) {
                return reject(new Error(ComposeUIErrors.CurrentChannelNotSet));
            } else {
                return resolve(this.currentChannel.broadcast(context));
            }
        });
    }

    public raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        return new Promise(async (resolve, reject) => {
            if (typeof app != 'string') {
                const messageId = Math.floor(Math.random() * 10000);
                const message = new Fdc3RaiseIntentRequest(messageId, window.composeui.fdc3.config!.instanceId!, intent, false, context, app);
                const responseFromService = await this.messageRouterClient.invoke(ComposeUITopic.raiseIntent(), JSON.stringify(message));
                
                //The backend should care about the functionality of the ResolverUI
                if (!responseFromService) {
                    return reject(new Error(ComposeUIErrors.NoAnswerWasProvided));
                }

                const response = <Fdc3RaiseIntentResponse>JSON.parse(responseFromService);

                if (response.error) {
                    return reject(new Error(response.error));
                }

                //At this point the AppMetadata should be set if it received no error.
                const intentResolution = new ComposeUIIntentResolution(response.messageId, this.messageRouterClient, response.intent!, response.appMetadata!);
                return resolve(intentResolution);
            }
            return reject(new Error("Using string type for app argument is not supported. Please use undefined | AppIdentifier types!"));
        });
    }

    //TODO
    public raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    public addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        return new Promise<ComposeUIIntentListener>(async (resolve, reject) => {
            const listener = new ComposeUIIntentListener(this.messageRouterClient, intent, window.composeui.fdc3.config!.instanceId!, handler);
            try {
                await listener.registerIntentHandler();
                const message = new Fdc3IntentListenerRequest(intent, window.composeui.fdc3.config!.instanceId!, "Subscribe");
                const response = await this.messageRouterClient.invoke(ComposeUITopic.addIntentListener(), JSON.stringify(message));
                if (!response) {
                    return reject(new Error(ComposeUIErrors.NoAnswerWasProvided));
                } else {
                    const result = <Fdc3IntentListenerResponse>JSON.parse(response);
                    if (result.error) {
                        await this.unsubscribe(listener);
                        return reject(new Error(result.error));
                    } else if (!result.stored) {
                        await this.unsubscribe(listener);
                        return reject(new Error(ComposeUIErrors.SubscribeFailure));
                    } else {
                        this.intentListeners.push(listener);
                        return resolve(listener);
                    }
                }
            } catch(err) {
                return reject(err);
            }
        });
    }

    public addContextListener(contextType?: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        return new Promise<ComposeUIContextListener>(async (resolve, reject) => {
            if (!this.currentChannel) {
                return reject(new Error(ComposeUIErrors.CurrentChannelNotSet));
            }

            if (contextType && typeof contextType != 'string') {
                handler = contextType;
                contextType = null;
            }

            const listener = <ComposeUIContextListener>await this.currentChannel!.addContextListener(contextType ?? null, handler!);
            const resultContext = await this.currentChannel!.getCurrentContext(contextType ?? undefined)
            
            await listener.handleContextMessage(resultContext);

            this.currentChannelListeners.push(listener);
            return resolve(listener);
        });
    }

    public getUserChannels(): Promise<Array<Channel>> {
        return Promise.resolve(this.userChannels);
    }

    //TODO: should return AccessDenied error when a channel object is denied?
    public joinUserChannel(channelId: string): Promise<void> {
        return new Promise<void>(async (resolve, reject) => {
            if (this.currentChannel) {
                return reject(new Error(ChannelError.AccessDenied));
            }

            let channel = this.userChannels.find(innerChannel => innerChannel.id == channelId);
            if (!channel) {
                try {
                    await this.invokeChannelCreationMessage(ComposeUITopic.joinUserChannel(), channelId, "user");
                    return resolve();
                } catch (error) {
                    return reject(error);
                }
            } else {
                this.currentChannel = channel;
                return resolve();
            }
        });
    }

    //TODO: should return AccessDenied error when a channel object is denied
    //TODO: should return a CreationFailed error when a channel cannot be created or retrieved (channelId failure)
    public getOrCreateChannel(channelId: string): Promise<Channel> {
        throw new Error("Not implemented.");
    }

    //TODO
    public createPrivateChannel(): Promise<PrivateChannel> {
        throw new Error("Not implemented");
    }

    public getCurrentChannel(): Promise<Channel | null> {
        return Promise.resolve(this.currentChannel ?? null);
    }

    //TODO: add messageRouter message that we are leaving the current channel to notify the backend.
    public leaveCurrentChannel(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            this.currentChannel = undefined;
            this.currentChannelListeners.forEach(listener => {
                const isUnsubscribed = listener.unsubscribe();
                if (!isUnsubscribed) {
                    return reject(new Error(`Listener couldn't unsubscribe. IsSubscribed: ${isUnsubscribed}, Listener: ${listener}`));
                }
            });
            this.currentChannelListeners = [];
            return resolve();
        });
    }

    //TODO: we should ask the backend to give the current appMetadata back
    public getInfo(): Promise<ImplementationMetadata> {
        return new Promise<ImplementationMetadata>(async (resolve, reject) => {
            const metadata = {
                fdc3Version: "2.0",
                provider: "ComposeUI",
                providerVersion: "0.1.0-alpha.1", //TODO: version check
                optionalFeatures: {
                    OriginatingAppMetadata: false,
                    UserChannelMembershipAPIs: false
                }
            };
            resolve(<ImplementationMetadata>metadata);
        });
    }

    //TODO
    public getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        throw new Error("Not implemented");
    }

    //TODO
    public getSystemChannels(): Promise<Channel[]> {
        return Promise.resolve(this.userChannels);
    }

    //TODO: Revisit for private channels
    public joinChannel(channelId: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if (this.currentChannel) {
                return reject(new Error(ChannelError.CreationFailed));
            }

            let channel = this.findChannel(channelId, "user");
            if (channel) {
                this.currentChannel = channel;
                return resolve();
            }

            channel = this.findChannel(channelId, "app");
            if (channel) {
                this.currentChannel = channel;
                return resolve();
            }

            channel = this.findChannel(channelId, "private");
            if (channel) {
                this.currentChannel = channel;
                return resolve();
            }

            if (!channel) {
                return reject(new Error(`No channel is found with id: ${channelId}`));
            }
        });
    }

    private findChannel(channelId: string, channelType: ChannelType): ComposeUIChannel | undefined {
        let channel;
        const predicate = (channel: Channel) => channel.id == channelId;

        switch (channelType) {
            case "app":
                channel = this.appChannels.find(predicate);
                break;
            case "private":
                channel = this.privateChannels.find(predicate);
                break;
            case "user":
                channel = this.userChannels.find(predicate);
                break;
        }

        return channel;
    }

    private addChannel(channel: ComposeUIChannel): void {
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

    private async invokeChannelCreationMessage(topic: string, channelId: string, channelType: ChannelType): Promise<void> {
        const message = JSON.stringify(new Fdc3FindChannelRequest(channelId, channelType));
        const response = await this.messageRouterClient.invoke(topic, message);
        if (response) {
            const fdc3Message = <Fdc3FindChannelResponse>JSON.parse(response);
            if (fdc3Message.error) {
                throw new Error(fdc3Message.error);
            }
            if (fdc3Message.found) {
                this.currentChannel = new ComposeUIChannel(channelId, channelType, this.messageRouterClient);
                this.addChannel(this.currentChannel);
            }
        }
    }

    private async unsubscribe(listener: ComposeUIIntentListener): Promise<void> {
        return new Promise(async (resolve, reject) => {
            try {
                await listener.unsubscribe();
            } catch (err) {
                console.log("Listener could not unsubscribe: ", err);
            }
            return resolve();
        });
    }
}