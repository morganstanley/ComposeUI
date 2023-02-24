#!/usr/bin/env node

"use strict";

import { executeScriptFile } from './executeScrtiptFile.js';

let fileName = process.argv.slice(2)[0];
executeScriptFile(fileName);