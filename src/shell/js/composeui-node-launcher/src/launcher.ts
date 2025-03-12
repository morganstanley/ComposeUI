import { execFile } from 'child_process';
import { WindowConfig } from './windowConfig';
import { fileURLToPath } from "node:url";

export class Launcher {
    private composeuiBinaryFileName = process.platform === 'win32' ? 'MorganStanley.ComposeUI.Shell.exe' : 'MorganStanley.ComposeUI.Shell'; 
    private composeuiBinaryFilePath = process.env.npm_config_composeui_binary_file_path || process.env.COMPOSEUI_BINARY_FILE_PATH || fileURLToPath(new URL(`./../dist/${this.composeuiBinaryFileName}`, import.meta.url));   

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
            
        const child = execFile(this.composeuiBinaryFilePath, argsArray, (error, stdout, stderr) => {
            console.log(stdout);
            if (error) {
                throw error;
            }
        });
    
        let exithandler = function() { process.exit() };
        child.on('close', exithandler);
    }
}
