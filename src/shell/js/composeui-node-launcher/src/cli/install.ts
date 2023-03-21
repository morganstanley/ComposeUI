#!/usr/bin/env node

"use strict";

import path from 'path';
import fs from 'fs';

import axios from 'axios';

function validatePlatform() {
    let platform = process.platform;
    console.log("platform", platform);
    console.log(process.arch);

    
    if (platform !== 'win32') {
      console.log('Unexpected platform or architecture:', process.platform, process.arch);
      process.exit(1);
    }
  
    return platform;
}

let cdnUrl = 'http://127.0.0.1:8080';
let downloadedFile = '';

function ensureDirectoryExistence(filePath: string) {
    let dirname = path.dirname(filePath);
    if (fs.existsSync(dirname)) {
      return true;
    }
    ensureDirectoryExistence(dirname);
    fs.mkdirSync(dirname);
  }

function downloadFile(dirToLoadTo: string) {
    console.log('downloadFile');
    //todo replace localhost download with the actual one.

    //todo skip download is binary is already there
    //todo zip: `composeui_${platform}.zip
    const fileName = 'ComposeUI-Shell.exe';
    
    const composeui_version = ''; //todo

    const tempDownloadedFile = path.resolve(dirToLoadTo, fileName);
    downloadedFile = tempDownloadedFile;
    console.log("downloadedFile: ", downloadedFile);

    //todo ${composeui_version}/
    const formattedDownloadUrl =`${cdnUrl}/${fileName}`;
    
    console.log('Downloading from file: ', formattedDownloadUrl);
    console.log('Saving to file:', downloadedFile);

    ensureDirectoryExistence(downloadedFile);
    axios.request({
        method: 'get',
        url: formattedDownloadUrl,
        responseType: 'stream'
    }).then(function (response) {
        response.data.pipe(fs.createWriteStream(downloadedFile))
    });
}

function install () {
    console.log("install");
    const tmpPath = process.cwd() + "/tmpdownload"; //todo
    console.log("tmpPath: ",tmpPath);
   
    validatePlatform();
    downloadFile(tmpPath);
}

install();