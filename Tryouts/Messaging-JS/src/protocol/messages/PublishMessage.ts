import { MessageBuffer } from "../../MessageBuffer";
import { MessageScope } from "../../MessageScope";
import { Message } from "./Message";

export interface PublishMessage extends Message {
    type: "Publish";
    topic: string;
    payload?: MessageBuffer;
    scope?: MessageScope;
    correlationId?: string;
}

export function isPublishMessage(message: Message): message is PublishMessage {
    return message.type == "Publish";
}
