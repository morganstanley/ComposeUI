export interface Error {
    name: string;
    message?: string;
}

export function isProtocolError(err: any): err is Error {
    return (typeof err === "object") && ("name" in err);
}