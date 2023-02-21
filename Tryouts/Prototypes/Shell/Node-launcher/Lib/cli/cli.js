#!/usr/bin/env node

import { execFile } from 'child_process';
import { resolve } from 'path';

let filename = process.argv.slice(2)[0];
let filePath= resolve(filename);

const child = execFile("node", [filePath], (error, stdout, stderr) => {
    console.log(stdout);
    if (error) {
        throw error;
    }
});
