import * as protocol from "../protocol";
import { MessageRouterError } from "./MessageRouterError";

export * from "./MessageRouterError";
export * from "./MessageRouterError.factory";
export * from "./DuplicateEndpointError";
export * from "./DuplicateRequestIdError";
export * from "./InvalidEndpointError";
export * from "./InvalidTopicError";
export * from "./UnknownEndpointError";
export * from "./ThrowHelper";

// TODO: Canonize standard error types
export const ErrorTypes = {
    default: "MorganStanley.ComposeUI.Exceptions.MessageRouterException",
    duplicateEndpoint: "MorganStanley.ComposeUI.Exceptions.DuplicateEndpointException",
    duplicateRequestId: "MorganStanley.ComposeUI.Exceptions.DuplicateRequestIdException",
    invalidEndpoint: "MorganStanley.ComposeUI.Exceptions.InvalidEndpointException",
    invalidTopic: "MorganStanley.ComposeUI.Exceptions.InvalidTopicException",
    unknownEndpoint: "MorganStanley.ComposeUI.Exceptions.UnknownEndpointException",
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


