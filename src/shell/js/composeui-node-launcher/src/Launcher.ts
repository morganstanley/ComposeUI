import { execFile } from 'child_process';
import { WindowConfig } from './WindowConfig';

export class Launcher {
    private processArgs(config?: WindowConfig) {
        let argsArray = [];
        if (config) {
            for (const [key, value] of Object.entries(config)) {
                argsArray.push(`--${key}=${value}`);
            }
        }

        return argsArray;
    }

    public launch(config?: WindowConfig) {
        let argsArray = this.processArgs(config);

        if (!config?.url) {
            throw new Error("At least the url must be specified!");
        } 
            const child = execFile("ComposeUI-Shell.exe", argsArray, (error, stdout, stderr) => {
                console.log(stdout);
                if (error) {
                    throw error;
                }
            });
        
            let exithandler = function() { process.exit() };
            child.on('close', exithandler);
    }
}
