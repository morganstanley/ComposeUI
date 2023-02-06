import { WindowConfig } from './WindowConfig.js';
export declare class BrowserWindow {
    private config;
    private launcher;
    constructor(config: WindowConfig);
    open(): void;
    loadUrl(url: string): void;
}
