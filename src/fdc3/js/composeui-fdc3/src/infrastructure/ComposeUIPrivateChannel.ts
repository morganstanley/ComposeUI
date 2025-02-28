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
import { MessageBuffer, MessageContext, MessageRouter, TopicMessage } from "@morgan-stanley/composeui-messaging-client";
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


    constructor(id: string, messageRouterClient: MessageRouter, isOriginalCreator: boolean) {
        super(id, "private", messageRouterClient);

        this.internalEventsTopic = ComposeUITopic.privateChannelInternalEvents(id);
        this.messageRouterClient.subscribe(this.internalEventsTopic, (m: TopicMessage) => this.internalEventsHandler(m))

        this.messageRouterClient.registerService(ComposeUITopic.privateChannelGetContextHandlers(id, isOriginalCreator),
            (e: string, p: MessageBuffer | undefined, c: MessageContext) => this.getContextListeners(e, p, c));
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

        this.messageRouterClient.publish(this.internalEventsTopic, JSON.stringify(new Fdc3PrivateChannelInternalEvent("disconnected")));
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

    private internalEventsHandler(message: TopicMessage) {
        if (
            this.disconnected
            || message.context.sourceId == this.messageRouterClient.clientId
            || !message.payload) {
            return;
        }

        const event = <Fdc3PrivateChannelInternalEvent>JSON.parse(message.payload);
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
        this.messageRouterClient.publish(this.internalEventsTopic, JSON.stringify(new Fdc3PrivateChannelInternalEvent("contextListenerAdded", contextType)));
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
            var listeners = await this.messageRouterClient.invoke(this.remoteContextListenersService, "{}");

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
        this.messageRouterClient.publish(this.internalEventsTopic, JSON.stringify(new Fdc3PrivateChannelInternalEvent("unsubscribed", contextType)));
    }

    private getContextListeners(_: string, _1: MessageBuffer | undefined, _2: MessageContext): MessageBuffer {
        const resultArray: Array<string | undefined> = [];
        this.contextHandlers.forEach(h => resultArray.push(h.contextType));
        return JSON.stringify(resultArray);
    }
}