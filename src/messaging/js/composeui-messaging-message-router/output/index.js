import { MessageRouterMessaging } from "./MessageRouterMessaging";
import { createMessageRouter } from "@morgan-stanley/composeui-messaging-client";
window.composeui.messaging.communicator = new MessageRouterMessaging(createMessageRouter());
