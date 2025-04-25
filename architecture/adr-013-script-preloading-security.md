<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title                                       |
| ------------- | ------------- | ------------- | ------------------------------------------- |
| ADR           | adr-013       | Accepted      | Security requirements for preloaded scripts |


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

Windows opened via window.open() or a similar mechanism are considered "popup windows". These are non-persistent and non-dockable. They are owned by the module that opened them and they are closed if the module is closed.

To minimize risks associated with untrusted content popup windows and iframes will not inherit the parent window's module identity or injected scripts. The same applies if the module tries to navigate to another domain.
If a new window needs access to these features it has to be opened as a separate module using container APIs (eg. FDC3 `raiseIntent` or `open`, or the Module Loader `start` API).

Depending on user feedback we may design way(s) for limited communication between a module main window and its child windows and iframes. This would still not grant direct access to the container but developers could have more flexibility in these kinds of interactions. We still need to consider access control to these features (e.g. allow-listing origins in the module manifest).

## Consequences

Safety concerns related to loading potentially hostile arbitrary urls with access to the container api are covered.
Modules that expect to open windows or make use of iframes that access the container features will not work with the container.
Even if we add communication options in the future we cannot ensure these features will be container agnostic.
