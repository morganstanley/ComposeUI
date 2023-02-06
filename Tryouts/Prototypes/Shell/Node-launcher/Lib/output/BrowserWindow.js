import { Launcher } from './Launcher.js';
export class BrowserWindow {
    config;
    launcher;
    constructor(config) {
        this.config = config;
        this.launcher = new Launcher();
    }
    open() {
        this.launcher.launch(this.config);
    }
    loadUrl(url) {
        this.config.url = url;
        this.launcher.launch(this.config);
    }
}
