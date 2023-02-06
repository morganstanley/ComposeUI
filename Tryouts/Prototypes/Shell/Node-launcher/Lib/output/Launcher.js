import { execFile } from 'child_process';
export class Launcher {
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
            const child = execFile("Shell.exe", argsArray, (error, stdout, stderr) => {
                console.log(stdout);
                if (error) {
                    throw error;
                }
            });
        }
    }
}
