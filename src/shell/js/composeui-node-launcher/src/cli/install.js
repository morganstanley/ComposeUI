#!/usr/bin/env node

"use strict";

import path from 'path';
import fs from 'fs';
import os from 'os';

import axios from 'axios';
import unzipper from '@deranged/unzipper';


//todo skip download is binary is already there

const DEFAULT_CDN_URL = 'https://github.com/morganstanley/ComposeUI/releases/download';

let cdnUrl = process.env.npm_config_composeui_cdn_url || process.env.COMPOSEUI_CDN_URL || DEFAULT_CDN_URL;
let downloadedFile = '';

let platform = validatePlatform();
const fileName = `composeui_${platform}.zip`;

const composeui_version = process.env.npm_config_composeui_version || process.env.COMPOSEUI_VERSION;
const skipDownload = process.env.npm_config_composeui_skip_download || process.env.COMPOSEUI_SKIP_DOWNLOAD;

if (skipDownload === 'true') {
    console.log('Found COMPOSEUI_SKIP_DOWNLOAD variable, skipping installation.');
    process.exit(0);
}

function validatePlatform() {
    let platform = process.platform;

    if (platform !== 'win32') {
      console.log('Unexpected platform or architecture:', process.platform, process.arch);
      process.exit(1);
    }
  
    return platform;
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
    const tempDownloadedFile = path.resolve(dirToLoadTo, fileName);
    downloadedFile = tempDownloadedFile;
    const formattedDownloadUrl =`${cdnUrl}/${composeui_version}/${fileName}`;
    
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

async function extractFile(zipPath, outPath) {
    let zipFile = `${zipPath}/${fileName}`;

    if (path.extname(zipFile) !== '.zip') {
        console.log('Skipping zip extraction - binary file found.');
        return;
      }
      console.log(`Extracting zip contents to ${outPath}.`);
      try {
        fs.createReadStream(zipFile).pipe(unzipper.Extract({ path: outPath }));
      } catch (error) {
        throw new Error('Error extracting archive: ' + error);
      }
}

function createTempFolder(){
    let tempFolderPath
    try {
        tempFolderPath = fs.mkdtempSync(path.join(os.tmpdir(), 'composeui-'));
      } catch (err) {
        console.error(err);
      }

      return tempFolderPath;
}

async function install() {
    const tmpPath = createTempFolder();
    const outPath = process.cwd() + "/dist";
    
    await downloadFile(tmpPath);
    await extractFile(tmpPath, outPath);
}

install();