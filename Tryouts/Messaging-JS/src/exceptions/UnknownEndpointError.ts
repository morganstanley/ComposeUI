import * as protocol from "../protocol";
import { ErrorTypes, MessageRouterError } from ".";

export class UnknownEndpointError extends MessageRouterError {
    constructor(err: { endpoint: string; } | protocol.Error) {
        const message = "endpoint" in err ? `Unknown endpoint: '${err.endpoint}'` : err.message;
        super({ type: ErrorTypes.unknownEndpoint, message });
    }
}
