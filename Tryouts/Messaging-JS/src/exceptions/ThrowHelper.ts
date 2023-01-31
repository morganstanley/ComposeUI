import { MessageRouterError } from "../MessageRouterError";
import { ErrorNames } from "../ErrorNames";

export class ThrowHelper {

    static duplicateEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.duplicateEndpoint, message: `Duplicate endpoint registration: '${endpoint}'`});
    }

    static duplicateRequestId(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.duplicateRequestId, message: "Duplicate request ID"});
    }

    static invalidEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.invalidEndpoint, message: `Invalid endpoint: '${endpoint}'`});
    }

    static invalidTopic(topic: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.invalidTopic, message: `Invalid topic: '${topic}'`});
    }

    static unknownEndpoint(endpoint: string): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.unknownEndpoint, message: `Unknown endpoint: ${endpoint}`});
    }

    static connectionClosed(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionClosed, message: "The connection has been closed"});
    }

    static connectionFailed(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionFailed, message: "Connection failed"});
    }

    static connectionAborted(): MessageRouterError {
        return new MessageRouterError({ name: ErrorNames.connectionAborted, message: "The connection dropped unexpectedly"});
    }

}
