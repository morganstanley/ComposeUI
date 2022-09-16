export type SuccessCallback = (data?: any) => void | Promise<any>;
export type FailCallback = (data?: any) => void;

type CallbackEntry = {
    success: SuccessCallback;
    fail?: FailCallback;
    onetime?: boolean;
};

export class CallbackStore {
    private store = new Map<string, CallbackEntry[]>();

    public invoke(key: string, type: 'success'|'fail', value?: any, success?: SuccessCallback, fail?: FailCallback) {
        const callbacks = this.store.get(key);
        if (callbacks) {
            for (let i = 0; i < callbacks.length; i++) {
                const callback = callbacks[i];
                let response: any;
                try {
                    response = callback[type]?.(value);
                } catch (ex: any) {
                    response = Promise.reject(ex);
                }
                
                const responsePromise = Promise.resolve(response);
                if (success) responsePromise.then(success);
                if (fail) responsePromise.catch(fail);

                if (callback.onetime) callbacks.splice(i--, 1);
            }
            if (callbacks.length === 0) this.store.delete(key);
        } else {
            console.warn(`No async callback found "${key}".`);
        }
    }

    /**
     * @returns true if this was the first callback for the given `key`
     */
    public add(key: string, success: SuccessCallback, fail?: FailCallback, onetime = false) {
        let isFirstCallback = false;
        let callbacks = this.store.get(key);
        if (!callbacks) {
            this.store.set(key, callbacks = []);
            isFirstCallback = true;
        }
        callbacks.push({ success, fail, onetime });
        return isFirstCallback;
    }

    /**
     * @returns true if this was the last callback removed for the given `key`
     */
    public remove(key: string, success: SuccessCallback, fail?: FailCallback) {
        const callbacks = this.store.get(key);
        if (!callbacks) return false;

        const idx = callbacks.findIndex(callback => callback.success === success && (!fail || callback.fail === fail));
        if (idx < 0) return false;

        callbacks.splice(idx, 1);
        const isLastCallback = callbacks.length === 0;
        
        if (isLastCallback) this.store.delete(key);

        return isLastCallback;
    }
}