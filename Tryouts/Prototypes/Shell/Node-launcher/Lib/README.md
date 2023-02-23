<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# @morgan-stanley/composeui-node-launcher

## Library

The library enables you to dynmically set properties for your window in your javascript code.

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

## CLI

The CLI enables you to execute your app with compose by executing the following command:

```
composeui myapp.js
```
