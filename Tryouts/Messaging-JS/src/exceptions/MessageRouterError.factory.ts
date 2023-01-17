import { DuplicateEndpointError, DuplicateRequestIdError, ErrorTypes, InvalidEndpointError, InvalidTopicError, MessageRouterError, UnknownEndpointError } from ".";
import * as protocol from "../protocol";

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