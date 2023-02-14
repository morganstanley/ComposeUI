import { Error, isProtocolError } from "./protocol/Error";

export class MessageRouterError extends Error {
    
    constructor(err: string | Error, message?: string, stack?: string) {
        let [name, msg] = isProtocolError(err) ? [err.name, err.message] : [err, message];
        super(msg);
        this.name = name;
        if (stack) {
            this.stack = stack;
        }
    }
}

export function createProtocolError(err: any): Error {
    if (typeof err === "string")
        return {
            name: "Error",
            message: err
        };

    return {
        name: err.name ?? "Error",
        message: err.message ?? `Unknown error (${err})`
    }
}
