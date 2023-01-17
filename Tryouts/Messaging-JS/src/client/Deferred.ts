export type Reject<T> = (reason?: any) => void;
export type Resolve<T> = (value: T | PromiseLike<T>) => void;

export class Deferred<T> {
    constructor() {
        this.promise = new Promise<T>(
            (resolve, reject) => {
                this.resolve = resolve;
                this.reject = reject;
            });
    }

    resolve: Resolve<T> = () => { };
    reject: Reject<T> = () => { };
    promise: Promise<T>;
}
