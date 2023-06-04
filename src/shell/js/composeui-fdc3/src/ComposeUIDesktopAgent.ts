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
    Context, 
    ContextHandler, 
    ContextTypes, 
    DesktopAgent, 
    ImplementationMetadata, 
    IntentHandler, 
    IntentResolution, 
    Listener, 
    PrivateChannel } from '@finos/fdc3'
import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIChannel } from './ComposeUIChannel';
import { ComposeUIChannelType } from './ComposeUIChannelType';
import { ComposeUIListener } from './ComposeUIListener';

export class ComposeUIDesktopAgent implements DesktopAgent {
    private appChannels: Array<Channel> = new Array<ComposeUIChannel>();
    private userChannels: Array<Channel> = new Array<ComposeUIChannel>();
    private privateChannels: Array<Channel> = new Array<ComposeUIChannel>();
    private currentChannel?: Channel;
    private messageRouterClient!: MessageRouter;
    private topicRoot: string = "composeui/fdc3/v2.0/userchannels/";
    private currentChannelListeners: Array<Listener> = new Array<ComposeUIListener>();

    constructor(name: string, messageRouterClient: MessageRouter) {
        this.messageRouterClient = messageRouterClient;
        var channel = new ComposeUIChannel(
            this.topicRoot + name + "/", 
            ComposeUIChannelType.User,
            this.messageRouterClient);
        this.addChannel(channel);
    }

    //TODO
    public open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        throw new Error("Not implemented");
    }

    //TODO
    public findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        throw new Error("Not implemented");
    }

    //TODO
    public findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        throw new Error("Not implemented");
    }

    //TODO
    public findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        throw new Error("Not implemented");
    }

    public broadcast(context: Context): Promise<void> {
        return new Promise((resolve, reject) => {
            if(this.currentChannel === undefined || this.currentChannel === null){
                reject(new Error("The current channel have not been set."));
            } else {
                resolve(this.currentChannel.broadcast(context));
            }
        })
    }

    //TODO
    public raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    //TODO
    public raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    //TODO
    public addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        throw new Error("Not implemented");
    }

    public addContextListener(contextType?: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        return new Promise<ComposeUIListener>(async(resolve, reject) => {
            if(this.currentChannel == null) {
                reject(new Error("The current channel is null or undefined"));
                return;
            }
            if(typeof contextType == 'function') {
                reject(new Error("The contextType was type of ContextHandler, which would use a deprecated function, please use string or null for contextType!"));
                return;
            }
            contextType = contextType as string ?? null; 
            
            //Resolving the task of subscription to the messageRouter server.
            const listener = <ComposeUIListener>await this.currentChannel?.addContextListener(contextType, handler!);
            const context = await this.currentChannel?.getCurrentContext(contextType); //TODO: what happens whe a broadcasted message arrives between 2 points
            await listener.handleContextMessage(context!);
            this.currentChannelListeners.push(listener);
            resolve(listener);
        });
    }

    public getUserChannels(): Promise<Array<Channel>> {
        return new Promise<Array<Channel>>((resolve, reject) => {
            resolve(this.userChannels);
        });
    }

    public joinUserChannel(channelId: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            let channel = this.userChannels.find(innerChannel => innerChannel.id ==  this.topicRoot + channelId + "/");
            if (channel == null || channel == undefined) {
                reject(new Error("Channel couldn't be found in user channels"));
            } else {
                this.currentChannel = channel;
                resolve();
            }
        });
    }

    public getOrCreateChannel(channelId: string): Promise<Channel> {
        return new Promise<Channel>((resolve, reject) => {
            let channel = this.userChannels.find(innerChannel => innerChannel.id == this.topicRoot + channelId + "/");
            if (channel == null || channel == undefined) {
                channel = new ComposeUIChannel(this.topicRoot + channelId + "/", ComposeUIChannelType.App, this.messageRouterClient); //TODO later
                this.addChannel(channel);
            }
            resolve(channel);
        });
    }

    //TODO
    public createPrivateChannel(): Promise<PrivateChannel> {
        throw new Error("Not implemented");
    }

    public getCurrentChannel(): Promise<Channel | null> {
        return new Promise<Channel>((resolve, reject) => {
            resolve(this.currentChannel!);
        });
    }

    public leaveCurrentChannel(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            this.currentChannel = undefined;
            resolve();
        });
    }

    public getInfo(): Promise<ImplementationMetadata> {
        return new Promise<ImplementationMetadata>((resolve, reject) => {
            const metadata = {
                fdc3Version: "2.0.0",
                provider: "ComposeUI",
                providerVersion: "1.0.1.alpha", //TODO: version check
                optionalFeatures: {
                    OriginatingAppMetadata: false,
                    UserChannelMembershipAPIs: false
                }
            } as ImplementationMetadata;
            resolve(metadata);
        });
    }

    //TODO
    public getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        throw new Error("Not implemented");
    }

    //TODO
    public getSystemChannels(): Promise<Channel[]> {
        throw new Error("Not implemented");
    }

    //TODO: Revisit for private channels
    public joinChannel(channelId: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if(this.currentChannel != null){
                reject(new Error("The current channel is already instantiated."));
                return;
            }

            let channel = this.findChannel(channelId, ComposeUIChannelType.User);
            if(channel != null && channel != undefined){
                this.currentChannel = channel;
                resolve();
                return;
            }

            channel = this.findChannel(channelId, ComposeUIChannelType.App);
            if(channel != null && channel != undefined){
                this.currentChannel = channel;
                resolve();
                return;
            }

            // channel = this.findChannel(channelId, ComposeUIChannelType.Private);
            // if(channel != null && channel != undefined){
            //     this.currentChannel = channel;
            //     resolve();
            //     return;
            // }

            if(channel == null || channel === undefined)
            {
                reject(new Error("No channel is found with id: " + channelId));
                return;
            }
        });
    }

    private findChannel(channelId: string, channelType: ComposeUIChannelType): Channel | undefined {
        let channel: Channel | undefined;
        const topic = this.topicRoot + channelId + "/";
        const predicate = (channel: Channel) => channel.id == topic;

        switch(channelType) {
            case ComposeUIChannelType.App:
                channel = this.appChannels.find(predicate);
                break;
            case ComposeUIChannelType.Private:
                channel = this.privateChannels.find(predicate);
                break;
            case ComposeUIChannelType.User:
                channel = this.userChannels.find(predicate);
                break;
        }

        return channel;
    }

    private addChannel(channel: Channel): void {
        if (channel == null) return;
        switch (channel.type) {
            case ComposeUIChannelType.App:
                this.appChannels.push(channel);
                break;
            case ComposeUIChannelType.User:
                this.userChannels.push(channel);
                break;
            case ComposeUIChannelType.Private:
                this.privateChannels.push(channel);
                break;
        }
    }
}