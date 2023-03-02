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

In order to set an icon for your application, set the _icon_ property when creating a new _BrowserWindow_. The url set for the icon must either be relative to the app url, or an http or https url. If the host part of the icon url is different from the app url, you must set the _COMPOSE_ALLOWED_IMAGE_SOURCES_ environment value to contain the allowed icon host(s). You may separate multiple allowed hosts with semicolons (;).

## CLI

The CLI enables you to execute your app with compose by executing the following command:

```
composeui myapp.js
```

### Local Development

If you're developing the CLI itself you need to execute the following command

```
npm link
```

in the `.\Tryouts\Prototypes\Shell\Node-launcher\Lib` folder