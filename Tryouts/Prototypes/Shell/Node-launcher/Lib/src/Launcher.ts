import { execFile } from 'child_process';
import { WindowConfig } from './WindowConfig';

export class Launcher {
    private processArgs(config?: WindowConfig) {
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

    public launch(config?: WindowConfig) {
        let argsArray = this.processArgs(config);
        //TODO replace after application is properly packaged and added to PATH
        var path = process.cwd() + "\\Tryouts\\Prototypes\\Shell\\bin\\Debug\\net6.0-windows\\";
        const child = execFile(path + "Shell.exe", argsArray, (error, stdout, stderr) => {
            console.log(stdout);
            if (error) {
                throw error;
            }
        });
    }
}
