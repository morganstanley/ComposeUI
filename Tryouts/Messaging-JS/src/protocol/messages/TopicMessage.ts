import { MessageBuffer } from "../../MessageBuffer";
import { MessageScope } from "../../MessageScope";
import { Message } from "./Message";

export interface TopicMessage extends Message {
    type: "Topic";
    topic: string;
    payload?: MessageBuffer;
    scope?: MessageScope;
    sourceId: string;
    correlationId?: string;
}

export function isTopicMessage(message: Message): message is TopicMessage {
    return message.type == "Topic";
}
