import { MessageBuffer } from "./MessageBuffer";
import { MessageContext } from "./MessageContext";

export interface TopicMessage {
    topic: string;
    payload?: MessageBuffer;
    context: MessageContext;
}
