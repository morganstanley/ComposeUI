import { MessageScope } from ".";

export interface MessageContext {
    sourceId: string;
    scope: MessageScope;
    correlationId?: string;
}
