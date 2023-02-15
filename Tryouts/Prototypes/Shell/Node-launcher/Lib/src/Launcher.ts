import { execFile } from 'child_process';
import { WindowConfig } from './WindowConfig';

export class Launcher {
    private processArgs(_config?: WindowConfig) {
        let argsArray = [];
        if (_config) {
            if (_config?.url) {
                argsArray.push(`--url=${_config?.url}`);
            }
       
            if (_config?.width) {
                argsArray.push(`--width=${_config?.width}`);
            }

            if (_config?.height) {
                argsArray.push(`--height=${_config?.height}`);
            }
            if (_config?.title) {
                argsArray.push(`--title=${_config?.title}`);
            }
        }

        return argsArray;
    }

    public launch(_config?: WindowConfig) {
        let argsArray = this.processArgs(_config);
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
