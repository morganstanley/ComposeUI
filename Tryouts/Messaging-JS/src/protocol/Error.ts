export interface Error {
    type: string;
    message?: string;
}

export function isProtocolError(err: any): err is Error {
    return "type" in err;
}