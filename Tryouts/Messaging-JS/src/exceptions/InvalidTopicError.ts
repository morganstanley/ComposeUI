import * as protocol from "../protocol";
import { ErrorTypes } from "./ErrorTypes";
import { MessageRouterError } from "./MessageRouterError";

export class InvalidTopicError extends MessageRouterError {
    constructor(err: { topic: string; } | protocol.Error) {
        const message = "topic" in err ? `Invalid topic: '${err.topic}'` : err.message;
        super({ type: ErrorTypes.invalidTopic, message });
    }
}
