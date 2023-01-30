import { MessageContext } from "./MessageContext";
import { MessageBuffer } from "./MessageBuffer";

export type MessageHandler = (endpoint: string, payload: MessageBuffer | undefined, context: MessageContext) => (MessageBuffer | Promise<MessageBuffer> | void | Promise<void>);
