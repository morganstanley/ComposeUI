import { WindowConfig } from './WindowConfig';
import { Launcher } from './launcher.js';

export class BrowserWindow {
    private launcher: Launcher;
  
    constructor(private config: WindowConfig) {
        this.launcher = new Launcher();
    }

    public open() {
        this.launcher.launch(this.config);
    }

    public loadUrl(url: string) {
        this.config.url = url
        this.launcher.launch(this.config);
    }
}