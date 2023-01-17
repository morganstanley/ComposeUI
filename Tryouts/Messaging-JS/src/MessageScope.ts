export class MessageScope {

    asString(): string {
        return this.value ?? "";
    }

    equals(other: MessageScope): boolean {
        return other.value === this.value;
    }

    isClientId(): boolean {
        return !!this.value && this.value.startsWith('@');
    }

    getClientId(): string | undefined {
        return this.isClientId() ? this.value?.slice(1) : undefined;
    }

    static readonly default = new MessageScope(null);
    static readonly application = this.default;

    static fromClientId(clientId: string): MessageScope {
        return new MessageScope("@" + clientId);
    }

    static parse(value: string): MessageScope {
        return new MessageScope(value);
    }

    private value: string | null = null;

    private constructor(value: string | null) {
        this.value = !!value ? value : null;
    }
}
