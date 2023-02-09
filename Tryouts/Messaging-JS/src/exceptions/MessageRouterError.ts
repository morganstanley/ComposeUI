import * as protocol from "../protocol";
import { ErrorTypes } from "./ErrorTypes";

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

export function createProtocolError(err: any): protocol.Error {
    
    if (err instanceof MessageRouterError) {
        return {
            type: err.type ?? ErrorTypes.default,
            message: err.message
        }
    }

    if (err instanceof Error) {
        return {
            type: err.name,
            message: err.message
        }
    }
    
    return {
        type: "Error",
        message: typeof err === "string" ? err : JSON.stringify(err) // TODO: not sure if this is the right way of stringifying an error
    }
}