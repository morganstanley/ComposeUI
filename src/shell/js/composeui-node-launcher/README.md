<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# @morgan-stanley/composeui-node-launcher

## Pre-requisites

### For the shell:
* [.Net Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.12-windows-x64-installer)
* [Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section)

## Library

The library enables you to dynamically set properties for your window in your javascript code.

```
function windowOpenExample() {
    const window = new BrowserWindow(
        {
            url: "https://github.com/morganstanley/composeui",
            title: "My Web App",
            width: 1600,
            height: 800
        });

    window.open();
}


windowOpenExample();
```

Or with loadUrl

```
function loadUrlExample() {
    const window = new BrowserWindow(
        {
            width: 1600,
            height: 800
        });

    window.loadUrl("https://github.com/morganstanley/composeui");
}

loadUrlExample();
```

In order to set an icon for your application, set the _icon_ property when creating a new _BrowserWindow_. The url set for the icon must either be relative to the app url, or an http or https url. If the host part of the icon url is different from the app url, you must set the _COMPOSE_ALLOWED_IMAGE_SOURCES_ environment value to contain the allowed icon host(s). You may separate multiple allowed hosts with semicolons (;).

## CLI

The CLI enables you to execute your app with ComposeUI by executing the following command:

```
composeui myapp.js
```
### Install.js

This script is downloading and extracting the necessary binaries from CDN during `npm install`.
By default it's downloading the binaries from the github tagged releases but the following variables can be overridden by setting an environment variable or including an .npmrc file:

CDN URL: `COMPOSEUI_CDN_URL` (or `npm_config_composeui_cdn_url`)

```
COMPOSEUI_CDN_URL='http://127.0.0.1:8080'
```

version: `COMPOSEUI_VERSION` (or `npm_config_composeui_version`)

```
COMPOSEUI_VERSION='0.1.0'
```

skip download: `COMPOSEUI_SKIP_DOWNLOAD` (or `npm_config_composeui_skip_download`)

```
COMPOSEUI_SKIP_DOWNLOAD='true'
```

location of the binary: `COMPOSEUI_BINARY_FILE_PATH` (or `npm_config_composeui_binary_file_path`)

```
COMPOSEUI_BINARY_FILE_PATH='path\to\binary\ComposeUI-Shell.exe'
```

### Local Development

#### Developing the CLI

If you're developing the CLI itself you need to execute the following command

```
npm link
```

in the `./src/shell/js/composeui-node-launcher/` folder.

#### Developing the install.js script

If you're developing the install.js script and would like to test if the binaries downloading and extracting as expected you can serve a folder with a name of the version containing a zip with the binaries locally (e.g with http-server), and set that link for the `COMPOSEUI_CDN_URL` environment variable.

For example:

```
COMPOSEUI_CDN_URL='http://127.0.0.1:8080'
```

#### Working with the locally compiled shell binary:

To achieve this you can set the `COMPOSEUI_BINARY_FILE_PATH` variable to point to the exe compiled by Visual Studio:

```
COMPOSEUI_BINARY_FILE_PATH='path\to\your\project\folder\ComposeUI\src\shell\dotnet\Shell\bin\Debug\net8.0-windows\ComposeUI-Shell.exe'
```