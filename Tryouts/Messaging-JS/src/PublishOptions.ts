import { MessageScope } from "./MessageScope";

export interface PublishOptions {
    scope?: MessageScope;
    correlationId?: string;
}
