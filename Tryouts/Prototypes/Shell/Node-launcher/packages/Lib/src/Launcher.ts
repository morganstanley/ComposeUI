import { execFile } from 'child_process';
import { WindowConfig } from './WindowConfig';

export class Launcher {
    private argsArray: string[] = [];

    constructor(_config?: WindowConfig ) { 
        console.debug(_config);
        
        this.processArgs(_config);
    }

    private processArgs(_config?: WindowConfig) {
        console.debug("processArgs")
        if (_config) {

            if (_config?.url !== undefined) {
                this.argsArray.push(`--url=${_config?.url}`);
            }
       
            if (_config?.width !== undefined) {
                this.argsArray.push(`--width=${_config?.width}`);
            }

            if (_config?.height !== undefined) {
                this.argsArray.push(`--height=${_config?.height}`);
            }
        }

        console.debug("\t[]", this.argsArray);
    }

    
    public launch() {
        //TODO replace after application is properly packaged and added to PATH
        var path = process.cwd() + "\\Tryouts\\Prototypes\\Shell\\bin\\Debug\\net6.0-windows\\";
        const child = execFile(path + "Shell.exe", this.argsArray, (error, stdout, stderr) => {
            console.log(stdout);
            if (error) {
                throw error;
            }
            console.log(stdout);
        });
    }
}
