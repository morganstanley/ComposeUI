import { MessageScope } from ".";

export interface InvokeOptions {
    scope?: MessageScope;
    correlationId?: string;
}
