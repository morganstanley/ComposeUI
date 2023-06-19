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
    PrivateChannel} from '@finos/fdc3'
import { MessageRouter } from '@morgan-stanley/composeui-messaging-client';
import { ComposeUIChannel } from './infrastructure/ComposeUIChannel';
import { ChannelType } from './infrastructure/ChannelType';
import { ComposeUIListener } from './infrastructure/ComposeUIListener';

//TODO sweep for non-standard errors
export class ComposeUIDesktopAgent implements DesktopAgent {
    private appChannels: ComposeUIChannel[] = [];
    private userChannels: ComposeUIChannel[] = [];
    private privateChannels: ComposeUIChannel[] = [];
    private currentChannel?: ComposeUIChannel;
    private messageRouterClient!: MessageRouter;
    private currentChannelListeners: ComposeUIListener[] = [];

    constructor(name: string, messageRouterClient: MessageRouter) {
        this.messageRouterClient = messageRouterClient;
        var channel = new ComposeUIChannel(
            name, 
            "user",
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
            if(!this.currentChannel){
                reject(new Error("The current channel have not been set."));
            } else {
                resolve(this.currentChannel.broadcast(context));
            }
        });
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
            if(!this.currentChannel) {
                reject(new Error("The current channel have not been set."));
                return;
            }
            if(typeof contextType == 'function') {
                reject(new Error("The contextType was type of ContextHandler, which would use a deprecated function, please use string or null for contextType!"));
                return;
            }
            contextType = contextType as string ?? null; 
            
            const listener = <ComposeUIListener>await this.currentChannel!.addContextListener(contextType, handler!);
            this.currentChannelListeners.push(listener);
            resolve(listener);
        });
    }

    public getUserChannels(): Promise<Array<Channel>> {
        return Promise.resolve(this.userChannels);
    }

    //TODO: should return AccessDenied error when a channel object is denied?
    public joinUserChannel(channelId: string): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            let channel = this.userChannels.find(innerChannel => innerChannel.id == channelId);
            if (!channel) {
                reject(new Error(ChannelError.NoChannelFound));
            } else {
                this.currentChannel = channel;
                resolve();
            }
        });
    }

    //TODO: should return AccessDenied error when a channel object is denied
    //TODO: should return a CreationFailed error when a channel cannot be created or retrieved (channelId failure)
    public getOrCreateChannel(channelId: string): Promise<Channel> {
        return new Promise<Channel>((resolve, reject) => {
            let channel = this.appChannels.find(innerChannel => innerChannel.id == channelId);
            if (!channel) {
                channel = new ComposeUIChannel(channelId, "app", this.messageRouterClient);
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
        return Promise.resolve(this.currentChannel!);
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
            if(this.currentChannel){
                reject(new Error(ChannelError.CreationFailed));
                return;
            }

            let channel = this.findChannel(channelId, "user");
            if(channel){
                this.currentChannel = channel;
                resolve();
                return;
            }

            channel = this.findChannel(channelId, "app");
            if(channel){
                this.currentChannel = channel;
                resolve();
                return;
            }

            channel = this.findChannel(channelId, "private");
            if(channel){
                this.currentChannel = channel;
                resolve();
                return;
            }

            if(!channel)
            {
                reject(new Error(`No channel is found with id: ${channelId}`));
                return;
            }
        });
    }

    private findChannel(channelId: string, channelType: ChannelType): ComposeUIChannel | undefined {
        let channel: ComposeUIChannel | undefined;
        const predicate = (channel: Channel) => channel.id == channelId;

        switch(channelType) {
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
}