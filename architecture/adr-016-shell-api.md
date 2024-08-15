<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

| Type          | ID            | Status        | Title                   |
| ------------- | ------------- | ------------- | ----------------------- |
| ADR           | adr-016       | Approved      | Shell API               |

# Architecture Decision Record: Shell API

## Context

We wish to ensure apps written for ComposeUI can run in any shell implementation and provide a consistent developer experience across shells. In order to achieve this, a fixed set of features for shell interaction need to be defined and made available for developers.

Another consideration we must make is how easy is it to bring existing applications into the ComposeUI environment. "Basic" web apps are expected to run out of the box, but apps targeted at running inside containers might leverage specific APIs. We need to consider API compatibility with other desktop containers as well as DesktopJS, an abstraction layer over multiple web container APIs we are maintaining.

### A list of features we want to provide with high priority:

#### Window management
Opening and closing of child windows with the opportunity to customize their properties like size, position, available controls, "topmostness" etc.
Child windows will need to fit the style of the Shell.
We will only support a single fully featured dockable window per module. Child windows opened via the shell will never be dockable and are considered temporary popups. Persistence for these is currently not planned, but we should avoid making it impossible.

#### Desktop notifications
We need the ability to display "basic" desktop notifications in a consistent style as well as custom notifications (e.g. a custom html content).
We need to be able to define callbacks when it is clicked, timeout values and programmatic closing logic.

#### Registering global hotkeys
We need the ability for apps to register global hotkeys and manage them.
A basic implementation would just provide a programmatic way to register commands.
An advanced implementation to consider could centralize these commands to the container and provide a container-wide UI for setting up hotkeys.

#### IPC
We already provide IPC functionality via the MessageRouter API and via FDC3. These fit our current IPC needs.

#### Logging
Centralized logging to a remote location is a highly desired feature for large organizations. 
 
### Future features we consider:
#### Management of chrome elements
Running apps might want to add dynamic functionality to the chrome (e.g. custom contextual ribbon buttons or shortcuts to certain functionality). 
#### Opening modules (Currently a priority via fdc3)
App developers may not wish to use fdc3 for all their apps or functionality, but they might want the ability to programatically open other modules
#### Container-consistent popups
Alerts and prompts are frequently used UI features. In order to provide a consistent experience, the ComposeUI shell should provide a way to display these in the style of the container instead of something unique to each app.
#### Propagating the container theme
Theme selection is a desired feature of the shell. Apps need the ability to apply a matching theme and get notified of switching the theme.

## Decision
We will implement the DesktopJS API as the base of our shell API.

The DesktopJS API is an abstraction layer above multiple desktop container technologies, already adopted by projects. Implementing it as part of the shell API enables applications targeting DesktopJS to run in ComposeUI without modifications necessary in the apps or DesktopJS.

The DesktopJS interface covers the following features we prioritize:
 - Window management
 - Notifications
 - Global hotkeys
 - IPC (We prefer FDC3 as the IPC interface, but the MessageRouter can be used directly and calling it via DesktopJS is trivial to implement)
 - Logging
 
It covers further features we need to consider how to implement:
 - Layout management (While this is a desired feature on the container level, saving specific module state is not in the initial feature set and will need future consideration)
 - Adding a tray icon
 - Screen info

## Alternatives considered 

### Implementing the API of another desktop container
This option sounds promising as applications targeted to that specific API would run in ComposeUI by default.
This would introduce a dependency on the development of an external interface - something that happens quite often, and breaking changes are not unheard of.
A further drawback is that we would force users of other containers to essentially port their apps to another container, something we consider undesirable compared to promoting the freedom of adopting DesktopJS.

### Designing our own API
The benefit of this option is that we would have the most control over our API but adoption by app developers is still an issue and designing a new API from scratch is a lot of unnecessary work. It would also impose work on implementing a DesktopJS adapter - a project maintained by us.

### Do not define a shell API
We did consider leaving the shell API dependent on the shell implementation. The benefits of this would be opening the possibility for implementing a richer shell experience with more features. We decided against this because we wish to enable portability of apps between shells and other containers.

## Consequences
We need to review priority of implementing the additional features covered by DesktopJS. When considering additional features not covered by DesktopJS, contributing to the DesktopJS project can be considered.