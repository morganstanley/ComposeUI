import { MessageRouterError } from "./MessageRouterError";

export class ThrowHelper {

    static connectionClosed(): MessageRouterError {
        return new MessageRouterError("The connection has been closed");
    }

}
