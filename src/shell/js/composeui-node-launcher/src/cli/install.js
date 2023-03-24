#!/usr/bin/env node

"use strict";

import path from 'path';
import fs from 'fs';

import axios from 'axios';

// @ts-ignore
import unzipper from '@deranged/unzipper';


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

let cdnUrl = 'http://127.0.0.1:8080'; // todo OR 
let downloadedFile = '';
const fileName = 'composeui.zip';
const composeui_version = ''; //todo
const skipDownload = process.env.npm_config_composeui_skip_download || process.env.COMPOSEUI_SKIP_DOWNLOAD;

if (skipDownload === 'true') {
    console.log('Found COMPOSEUI_SKIP_DOWNLOAD variable, skipping installation.');
    process.exit(0);
}

function ensureDirectoryExistence(filePath) {
    let dirname = path.dirname(filePath);
    if (fs.existsSync(dirname)) {
      return true;
    }
    ensureDirectoryExistence(dirname);
    fs.mkdirSync(dirname);
  }

async function downloadFile(dirToLoadTo) {
    //todo replace localhost download with the actual one.
    //todo skip download is binary is already there

    //todo zip: `composeui_${platform}.zip

    const tempDownloadedFile = path.resolve(dirToLoadTo, fileName);
    downloadedFile = tempDownloadedFile;
    console.log("downloadedFile: ", downloadedFile);

    //todo include composeui_version/
    const formattedDownloadUrl =`${cdnUrl}/${fileName}`;
    
    console.log('Downloading from file: ', formattedDownloadUrl);
    console.log('Saving to file:', downloadedFile);

    ensureDirectoryExistence(downloadedFile);
    await axios.request({
        method: 'get',
        url: formattedDownloadUrl,
        responseType: 'stream'
    }).then(function (response) {
        response.data.pipe(fs.createWriteStream(downloadedFile))
    });
}

function extractFile (zipPath, outPath ) {
    let path = `${zipPath}/${fileName}`;

    fs.createReadStream(path)
        .pipe(unzipper.Extract({ path: outPath }));
}

async function install () {
    const tmpPath = process.cwd() + "/tmpdownload"; //todo
    console.log("tmpPath: ",tmpPath);

    validatePlatform();
    await downloadFile(tmpPath);
    extractFile(tmpPath, tmpPath);
}

install();