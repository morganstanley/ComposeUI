import * as protocol from "../protocol";
import { DuplicateEndpointError } from "./DuplicateEndpointError";
import { DuplicateRequestIdError } from "./DuplicateRequestIdError";
import { ErrorTypes } from "./ErrorTypes";
import { InvalidEndpointError } from "./InvalidEndpointError";
import { InvalidTopicError } from "./InvalidTopicError";
import { MessageRouterError } from "./MessageRouterError";
import { UnknownEndpointError } from "./UnknownEndpointError";

// Don't merge this into MessageRouterError.ts. That causes a circular reference and a runtime error saying MessageRouterError is not a constructor.

MessageRouterError.fromProtocolError = (error: protocol.Error): MessageRouterError => {
    switch (error.type) {
        case ErrorTypes.duplicateEndpoint:
            return new DuplicateEndpointError(error);
        case ErrorTypes.duplicateRequestId:
            return new DuplicateRequestIdError(error);
        case ErrorTypes.invalidEndpoint:
            return new InvalidEndpointError(error);
        case ErrorTypes.invalidTopic:
            return new InvalidTopicError(error);
        case ErrorTypes.unknownEndpoint:
            return new UnknownEndpointError(error);
    }

    return new MessageRouterError(error);
}