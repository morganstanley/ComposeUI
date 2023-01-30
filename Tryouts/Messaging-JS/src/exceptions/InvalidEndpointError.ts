import * as protocol from "../protocol";
import { ErrorTypes } from "./ErrorTypes";
import { MessageRouterError } from "./MessageRouterError";

export class InvalidEndpointError extends MessageRouterError {
    constructor(err: { endpoint: string; } | protocol.Error) {
        const message = "endpoint" in err ? `Invalid endpoint: '${err.endpoint}'` : err.message;
        super({ type: ErrorTypes.invalidEndpoint, message });
    }
}
