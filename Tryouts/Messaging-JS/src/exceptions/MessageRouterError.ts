import * as protocol from "../protocol";
import { ErrorTypes } from ".";

export class MessageRouterError extends Error {

    constructor(error: string | protocol.Error) {
        const [type, message] = typeof error === "string"
            ? [ErrorTypes.default, error]
            : [error.type, error.message];

        super(message);

        this.type = type;
    }

    readonly type?: string;

    static fromProtocolError(error: protocol.Error): MessageRouterError {
        throw new Error("Not implemented");
    }
}