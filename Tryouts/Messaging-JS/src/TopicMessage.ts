import { MessageContext, MessageBuffer } from ".";

export interface TopicMessage {
    topic: string;
    payload?: MessageBuffer;
    context: MessageContext;
}
