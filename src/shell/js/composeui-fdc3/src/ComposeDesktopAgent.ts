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


import { AppIdentifier, AppIntent, AppMetadata, Channel, Context, ContextHandler, DesktopAgent, ImplementationMetadata, IntentHandler, IntentResolution, Listener, PrivateChannel } from '@finos/fdc3'

export class ComposeDesktopAgent implements DesktopAgent {
    public open(app?: string | AppIdentifier, context?: Context): Promise<AppIdentifier> {
        throw new Error("Not implemented");
    }

    public findIntent(intent: string, context?: Context, resultType?: string): Promise<AppIntent> {
        throw new Error("Not implemented");
    }

    public findIntentsByContext(context: Context, resultType?: string): Promise<Array<AppIntent>> {
        throw new Error("Not implemented");
    }

    public findInstances(app: AppIdentifier): Promise<Array<AppIdentifier>> {
        throw new Error("Not implemented");
    }

    public broadcast(context: Context): Promise<void> {
        throw new Error("Not implemented");
    }

    public raiseIntent(intent: string, context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    public raiseIntentForContext(context: Context, app?: string | AppIdentifier): Promise<IntentResolution> {
        throw new Error("Not implemented");
    }

    public addIntentListener(intent: string, handler: IntentHandler): Promise<Listener> {
        throw new Error("Not implemented");
    }

    public addContextListener(contextType: string | null | ContextHandler, handler?: ContextHandler): Promise<Listener> {
        throw new Error("Not implemented");
    }

    public getUserChannels(): Promise<Array<Channel>> {
        throw new Error("Not implemented");
    }

    public joinUserChannel(channelId: string): Promise<void> {
        throw new Error("Not implemented");
    }

    public getOrCreateChannel(channelId: string): Promise<Channel> {
        throw new Error("Not implemented");
    }

    public createPrivateChannel(): Promise<PrivateChannel> {
        throw new Error("Not implemented");
    }

    public getCurrentChannel(): Promise<Channel | null> {
        throw new Error("Not implemented");
    }

    public leaveCurrentChannel(): Promise<void> {
        throw new Error("Not implemented");
    }

    public getInfo(): Promise<ImplementationMetadata> {
        throw new Error("Not implemented");
    }

    public getAppMetadata(app: AppIdentifier): Promise<AppMetadata> {
        throw new Error("Not implemented");
    }

    public getSystemChannels(): Promise<Channel[]> {
        throw new Error("Not implemented");
    }

    public joinChannel(channelId: string): Promise<void> {
        throw new Error("Not implemented");
    }
}