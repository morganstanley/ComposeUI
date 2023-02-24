import { execFile } from 'child_process';
import { resolve } from 'path';

"use strict";

export function executeScriptFile(fileName: string){
    console.log("executeScriptFile", fileName);
    if (fileName) {
        let filePath= resolve(fileName);

        const child = execFile("node", [filePath], (error, stdout, stderr) => {
            console.log(stdout);
            if (error) {
                throw error;
            }
        });
    } else {
        throw new Error("Specify filename.");
    } 
}