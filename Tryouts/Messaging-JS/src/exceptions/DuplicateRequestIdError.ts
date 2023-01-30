import * as protocol from "../protocol";
import { ErrorTypes } from "./ErrorTypes";
import { MessageRouterError } from "./MessageRouterError";

export class DuplicateRequestIdError extends MessageRouterError {
    constructor(err: undefined | protocol.Error) {
        const message = err?.message ?? "Duplicate request ID";
        super({ type: ErrorTypes.duplicateRequestId, message });
    }
}
