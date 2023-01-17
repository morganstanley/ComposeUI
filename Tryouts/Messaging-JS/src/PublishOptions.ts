import { MessageScope } from ".";

export interface PublishOptions {
    scope?: MessageScope;
    correlationId?: string;
}
