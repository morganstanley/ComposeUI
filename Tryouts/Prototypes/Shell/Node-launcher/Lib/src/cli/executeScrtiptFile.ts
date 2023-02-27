import { execFile, fork } from 'child_process';
import { resolve } from 'path';

"use strict";

export function executeScriptFile(fileName: string){
    console.log("executeScriptFile", fileName);
    if (fileName) {
        let filePath: string = resolve(fileName);
        const child = fork(filePath);
    } else {
        throw new Error("Specify filename.");
    }
}