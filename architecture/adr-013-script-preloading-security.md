<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title                                       |
| ------------- | ------------- | ------------- | ------------------------------------------- |
| ADR           | adr-013       | Draft         | Security requirements for preloaded scripts |


# Security requirements for preloaded scripts

## Context

For some features to work, we are preloading JavaScript code into web views (eg. Message Router configuration).
These scripts might add configuration or code that can grant access to protected APIs meant for trusted modules only.
Since some web modules create iframes and windows using `window.open()`, we must specify what and when to load into these views.

This ADR answers the following questions:

1. When a web module creates a new web view, should that view inherit the identity of the module from the top-level window?
2. Should we preload scripts into dynamically created web views?

Any solution must be consistent with web security principles such as the [Same Origin Policy](https://developer.mozilla.org/en-US/docs/Web/Security/Same-origin_policy),
and in general, should behave as expected by web developers.

### Scenarios to consider

#### 1. Trusted URL loaded into a popup window

In this scenario, the web view wants to create a second window, with the same APIs accessible as for the parent window.
The new window is treated as a new instance of the same module.

The new window might navigate away to another URL that is not trusted, and after navigation should
not be able to interact with restricted APIs, even if at some point it navigates back to a trusted URL.

#### 2. Trusted URL loaded into an iframe

Similarly to scenario 1, the iframe is a new instance of the same module, and has access to the same protected APIs.

The frame might navigate away to another URL that is not trusted, and after navigation should
not be able to interact with restricted APIs, even if at some point it navigates back to a trusted URL.

#### 3. External URL loaded into a popup window

In this scenario, the popup window should behave like a vanilla browser window, without access to protected APIs.

#### 4. External URL loaded into an iframe

Similarly to scenario 3, the iframe should not be able to interact with protected APIs.

#### 5. iframe as a layout element

The iframe is part of the page, scripts running in its context should be able to access protected APIs as if they
were running in the parent window, eg. the parent window should not receive broadcasts from the iframe.
If the iframe navigates to an untrusted URL, it should not be able to interact with protected APIs, 
even if it navigates back to a trusted URL.

### Behavior in other containers

#### Electron

Electron provides the [`BrowserWindow`](https://www.electronjs.org/docs/latest/api/browser-window) API for dynamically creating and interacting with web windows.
A single preload script can be specified for each window via the `preload` property of the options objects. Preload scripts can also be customized
for windows created via `window.open`, see https://www.electronjs.org/docs/latest/api/window-open

Regarding our scenarios, Electron does not automatically preload any scripts or provide access to protected APIs (eg. via node integration) for
windows created in the renderer process without additional parameters.

## Decision

TBA

To minimize risks associated with untrusted content, popup windows and iframes will not inherit the parent window's module identity or injected scripts.
When a module wants to pop up another window, it has to do so using container APIs (eg. FDC3 `raiseIntent` or `open`, or the Module Loader `start` API).

We provide fine-grained control over script injection in web views. Plugins can examine the originating module's manifest, 
the target URL and its context (eg. a popup window or iframe) when determining what to preload. Frames without reference to a module instance
will not receive any preloaded scripts unless some shell features require them.

## Consequences

TBA