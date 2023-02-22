import { BrowserWindow } from '@morgan-stanley/composeui-node-launcher';

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