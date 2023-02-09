import { Message } from "./Message";

export interface UnsubscribeMessage extends Message {
    type: "Unsubscribe";
    topic: string;
}

export function isUnsubscribeMessage(message: Message): message is UnsubscribeMessage {
    return message.type == "Unsubscribe";
}
