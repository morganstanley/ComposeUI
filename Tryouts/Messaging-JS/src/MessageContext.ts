import { MessageScope } from "./MessageScope";

export interface MessageContext {
    sourceId: string;
    scope: MessageScope;
    correlationId?: string;
}
