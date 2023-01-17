import { MessageBuffer, MessageContext } from ".";

export type MessageHandler = (endpoint: string, payload: MessageBuffer | undefined, context: MessageContext) => (MessageBuffer | Promise<MessageBuffer> | void | Promise<void>);
