import { Launcher } from '@compose/node-launcher';

function createWindow() {
    
    const win = new Launcher(
        {
            url: "https://github.com/morganstanley/composeui",
            width: 1600,
            height: 800
        });

    win.launch();

    //const win2 = new Launcher();
    //win2.launch()
    
}

createWindow();