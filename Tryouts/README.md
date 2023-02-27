<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# Tryouts
=========

Tryouts folder content is very similar to that of src folder, but it can be less final and less perfect. 

Tryouts folder contains

1. Core folder containing non-visual multiplatform code that can be re-used between various projects e.g. between various prototypes and plugins. One should NOT add  REFERENCES TO VISUAL PROJECTS OR PACKAGES to the projects within Core. 
2. Visuals folder contains Visual functionality re-usuable between various prototypes and plugins, but not necessarily multiplatform. Its Avalonia folder contains Avalonia functionality (which is multiplatform), but in the future we might add also e.g. a WPF folder which will contain window only WPF re-usable functionality.
3. Plugins folder contains either application plugins (executables separate from the shell that can be displayed by the shell) or dynamically loaded plugins. Main and prototype projects do not directly depend on the dynamically loaded plugins - instead they load them dynamically and deal with their functionality via interfaces. Those interfaces allowing the dynimcally loaded plugins to interact with each other, with the shell and other processes are defined within Abstractions project of the Core. 
4. Prototypes folder with visual and non-visual
prototypes for various features (check Prototype Driven Development or PDD - https://www.codeproject.com/Articles/5324212/Prototype-Driven-Development-PDD). Each prototype is a window or console application with dependencies on the re-usable projects. 

## Lerna
=========

The javascript dependencies are managed by a lerna monorepo.

### Run scripts

In the Tryouts folder
npm i

Build all modules:
npx lerna run build --stream

Test all modules:
npx lerna run test --stream

(If you don't want a detailed log, you can execute these without --stream)

Building a sepecific module:
npx lerna run build --stream --scope=@morgan-stanley/composeui-messaging-client

List all modules in the workspace
npx lerna list

### Docs

For mor info check the [documentation](https://lerna.js.org/docs/api-reference/commands).