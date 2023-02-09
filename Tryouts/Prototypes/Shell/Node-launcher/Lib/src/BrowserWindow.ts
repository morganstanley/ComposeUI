import { WindowConfig } from './WindowConfig.js';
import { Launcher } from './Launcher.js';

export class BrowserWindow {
    private launcher: Launcher;
  
    constructor(private _config: WindowConfig) {
        this.launcher = new Launcher();
    }

    public open() {
        this.launcher.launch(this._config);
    }

    public loadUrl(_url: string) {
        this._config.url=_url
        this.launcher.launch(this._config);
    }
}