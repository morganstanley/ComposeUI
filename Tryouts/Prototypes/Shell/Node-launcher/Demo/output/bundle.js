import { execFile } from 'child_process';

class Launcher {
    processArgs(config) {
        let argsArray = [];
        if (config) {
            if (config?.url) {
                argsArray.push(`--url=${config?.url}`);
            }
            if (config?.width) {
                argsArray.push(`--width=${config?.width}`);
            }
            if (config?.height) {
                argsArray.push(`--height=${config?.height}`);
            }
            if (config?.title) {
                argsArray.push(`--title=${config?.title}`);
            }
        }
        return argsArray;
    }
    launch(config) {
        let argsArray = this.processArgs(config);
        if (argsArray.length === 0) {
            throw new Error("Specify at least one argument.");
        }
        else {
            execFile("Shell.exe", argsArray, (error, stdout, stderr) => {
                console.log(stdout);
                if (error) {
                    throw error;
                }
            });
        }
    }
}

class BrowserWindow {
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

function windowOpenExample() {
    const window = new BrowserWindow(
        {
            url: "https://github.com/morganstanley/composeui",
            title: "My Web App",
            width: 1600,
            height: 800
        });

    window.open();
}

function loadUrlExample() {
    const window = new BrowserWindow(
        {
            width: 1600,
            height: 800
        });

    window.loadUrl("https://github.com/morganstanley/fdc3-dotnet");
}

windowOpenExample();
loadUrlExample();
