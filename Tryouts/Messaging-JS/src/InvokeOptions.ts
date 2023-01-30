import { MessageScope } from "./MessageScope";

export interface InvokeOptions {
    scope?: MessageScope;
    correlationId?: string;
}
