import { Message } from "./Message";

export interface SubscribeMessage extends Message {
    type: "Subscribe";
    topic: string;
}

export function isSubscribeMessage(message: Message): message is SubscribeMessage {
    return message.type == "Subscribe";
}
