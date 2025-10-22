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

import { Context, ContextHandler, Listener, PrivateChannel } from "@finos/fdc3";
import { JsonMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
import { ComposeUIChannel } from "./ComposeUIChannel";
import { ComposeUIContextListener } from "./ComposeUIContextListener";
import { ComposeUITopic } from "./ComposeUITopic";
import { PrivateChannelContextListenerEventListener } from "./PrivateChannelContextListenerEventListener";
import { PrivateChannelDisconnectEventListener } from "./PrivateChannelDisconnectEventListener"
import { Fdc3PrivateChannelInternalEvent } from "./messages/Fdc3PrivateChannelInternalEvent";

export class ComposeUIPrivateChannel extends ComposeUIChannel implements PrivateChannel {
    private disconnected = false;

    private addContextListenerHandlers: Array<PrivateChannelContextListenerEventListener> = [];
    private unsubscribeHandlers: Array<PrivateChannelContextListenerEventListener> = [];
    private disconnectHandlers: Array<PrivateChannelDisconnectEventListener> = [];

    private readonly contextHandlers: Array<ComposeUIContextListener> = [];

    private readonly internalEventsTopic: string;
    private readonly remoteContextListenersService: string;

    constructor(id: string, private fdc3InstanceId: string, jsonMessaging: JsonMessaging, isOriginalCreator: boolean) {
        super(id, "private", jsonMessaging);

        this.internalEventsTopic = ComposeUITopic.privateChannelInternalEvents(id);

        this.jsonMessaging.subscribeJson<Fdc3PrivateChannelInternalEvent>(this.internalEventsTopic, (m: Fdc3PrivateChannelInternalEvent) => this.internalEventsHandler(m));

        this.jsonMessaging.registerJsonService<any, Array<string | undefined>>(
            ComposeUITopic.privateChannelGetContextHandlers(id, isOriginalCreator), 
            this.getContextListeners);

        this.remoteContextListenersService = ComposeUITopic.privateChannelGetContextHandlers(id, !isOriginalCreator);
    }

    public onAddContextListener(handler: (contextType?: string) => void): Listener {
        if (this.disconnected) {
            throw new Error("Channel disconnected");
        }

        var listener = new PrivateChannelContextListenerEventListener(handler, (x) => this.removeAddContextListenerHandler(x));

        this.addContextListenerHandlers.push(listener);

        // This only triggers the execution, but it will be done asynchronously
        this.executeForRemoteContextHandlers(listener);

        return listener;
    }

    public onUnsubscribe(handler: (contextType?: string) => void): Listener {
        if (this.disconnected) {
            throw new Error("Channel disconnected");
        }

        var listener = new PrivateChannelContextListenerEventListener(handler, (x) => this.removeUnsubscribeListenerHandler(x));
        this.unsubscribeHandlers.push(listener);
        return listener;
    }

    public onDisconnect(handler: () => void): Listener {
        if (this.disconnected) {
            throw new Error("Channel disconnected");
        }

        var listener = new PrivateChannelDisconnectEventListener(handler, (x) => this.removeDisconnectListenerHandler(x));
        this.disconnectHandlers.push(listener);
        return listener;
    }

    public disconnect(): void {
        if (this.disconnected) { return; }

        this.disconnected = true;
        this.addContextListenerHandlers.forEach(l => l.unsubscribeInternal(false));
        this.unsubscribeHandlers.forEach(l => l.unsubscribeInternal(false));
        this.disconnectHandlers.forEach(l => l.unsubscribeInternal(false));

        this.addContextListenerHandlers = [];
        this.unsubscribeHandlers = [];
        this.disconnectHandlers = [];

        this.contextHandlers.forEach(l => l.unsubscribe());

        const request = new Fdc3PrivateChannelInternalEvent("disconnected", this.fdc3InstanceId);

        this.jsonMessaging.publishJson<Fdc3PrivateChannelInternalEvent>(this.internalEventsTopic, request);
    }

    public broadcast(context: Context): Promise<void> {
        if (this.disconnected) {
            throw new Error("Channel disconnected");
        }

        return super.broadcast(context)
    }

    public addContextListener(contextType: string | null, handler: ContextHandler): Promise<Listener>;
    public addContextListener(handler: ContextHandler): Promise<Listener>;
    public async addContextListener(contextType: any, handler?: any): Promise<Listener> {
        var listener = <ComposeUIContextListener>(await super.addContextListener(contextType, handler));
        listener.setUnsubscribeCallback(l => this.removeContextHandler(l));
        this.contextHandlers.push(listener);

        this.fireContextHandlerAdded(contextType ?? undefined);

        return listener;
    }

    private internalEventsHandler(event: Fdc3PrivateChannelInternalEvent) {
        if (
            this.disconnected) {
            return;
        }

        if (event.instanceId == this.fdc3InstanceId) {
            return;
        }

        switch (event.event) {
            case "contextListenerAdded":
                this.addContextListenerHandlers.forEach(handler => {
                    handler.execute(event.contextType);
                });
                break;
            case "unsubscribed":
                this.unsubscribeHandlers.forEach(handler =>
                    handler.execute(event.contextType));
                break;
            case "disconnected":
                this.disconnectHandlers.forEach(handler =>
                    handler.execute());
                break;
        }
    }

    private fireContextHandlerAdded(contextType: string | undefined) {
        const request = new Fdc3PrivateChannelInternalEvent("contextListenerAdded", this.fdc3InstanceId, contextType);
        this.jsonMessaging.publishJson<Fdc3PrivateChannelInternalEvent>(this.internalEventsTopic, request);
    }

    private removeAddContextListenerHandler(listener: PrivateChannelContextListenerEventListener) {
        var idx = this.addContextListenerHandlers.indexOf(listener);
        this.addContextListenerHandlers.splice(idx)
    }

    private removeUnsubscribeListenerHandler(listener: PrivateChannelContextListenerEventListener) {
        var idx = this.unsubscribeHandlers.indexOf(listener);
        this.unsubscribeHandlers.splice(idx)
    }

    private removeDisconnectListenerHandler(listener: PrivateChannelDisconnectEventListener) {
        var idx = this.disconnectHandlers.indexOf(listener);
        this.disconnectHandlers.splice(idx);
    }

    private async executeForRemoteContextHandlers(handler: PrivateChannelContextListenerEventListener): Promise<void> {
        let remoteListeners: Array<string | undefined>;
        try {
            var listeners = await this.jsonMessaging.invokeJsonService<string, string>(this.remoteContextListenersService, "{}");

            remoteListeners = listeners ? JSON.parse(listeners) : [];
        }
        catch (e) {
            // If this fails, the other side didn't connect yet, so nothing to do
            return;
        }

        remoteListeners.forEach(l => handler.execute(l));
    }

    private removeContextHandler(listener: ComposeUIContextListener) {
        // If we are disconnected, don't mutate the array as we are iterating on it
        if (!this.disconnected) {
            var idx = this.contextHandlers.indexOf(listener);
            this.contextHandlers.splice(idx);
        }
        this.fireUnsubscribed(listener.contextType);
    }

    private fireUnsubscribed(contextType: string | undefined): void {
        const request = new Fdc3PrivateChannelInternalEvent("unsubscribed", this.fdc3InstanceId, contextType);
        this.jsonMessaging.publishJson<Fdc3PrivateChannelInternalEvent>(this.internalEventsTopic, request);
    }

    private getContextListeners(): Promise<Array<string | undefined>> {
        const resultArray: Array<string | undefined> = [];
        this.contextHandlers.forEach(h => resultArray.push(h.contextType));
        return Promise.resolve(resultArray);
    }
}
