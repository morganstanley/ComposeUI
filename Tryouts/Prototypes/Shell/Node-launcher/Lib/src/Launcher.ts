import { execFile } from 'child_process';
import { WindowConfig } from './WindowConfig';

export class Launcher {
    private processArgs(config?: WindowConfig) {
        let argsArray = [];
        if (config) {
            if (config?.title) {
                argsArray.push(`--title=${config.title}`);
            }
            if(config?.icon) {
                argsArray.push(`--icon=${config.icon}`)
            }
            if (config?.url) {
                argsArray.push(`--url=${config.url}`);
            }
       
            if (config?.width) {
                argsArray.push(`--width=${config.width}`);
            }

            if (config?.height) {
                argsArray.push(`--height=${config.height}`);
            }            
        }

        return argsArray;
    }

    public launch(config?: WindowConfig) {
        let argsArray = this.processArgs(config);

        if (!config?.url) {
            throw new Error("At least the url must be specified!");
        } 
            const child = execFile("Shell.exe", argsArray, (error, stdout, stderr) => {
                console.log(stdout);
                if (error) {
                    throw error;
                }
            });
        
    }
}