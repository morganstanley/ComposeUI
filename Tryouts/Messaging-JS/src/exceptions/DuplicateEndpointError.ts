import * as protocol from "../protocol";
import { ErrorTypes } from "./ErrorTypes";
import { MessageRouterError } from "./MessageRouterError";

export class DuplicateEndpointError extends MessageRouterError {
    constructor(err: { endpoint: string; } | protocol.Error) {
        const message = "endpoint" in err ? `Duplicate endpoint: '${err.endpoint}'` : err.message;
        super({ type: ErrorTypes.duplicateEndpoint, message });
    }
}
