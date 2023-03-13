<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

ComposeUI
=========

![Lifecycle Incubating](https://badgen.net/badge/Lifecycle/Incubating/yellow) [![Build Status](https://github.com/MorganStanley/ComposeUI/actions/workflows/continuous-integration.yml/badge.svg?event=push)](https://github.com/MorganStanley/ComposeUI/actions/workflows/continuous-integration.yml)
[![NuGet](https://img.shields.io/nuget/v/MorganStanley.ComposeUI.svg?style=flat)](https://www.nuget.org/packages/MorganStanley.ComposeUI/)
[![codecov](https://codecov.io/gh/MorganStanley/ComposeUI/branch/main/graph/badge.svg)](https://codecov.io/gh/MorganStanley/ComposeUI)
[![GitHub Repo stars](https://img.shields.io/github/stars/morganstanley/ComposeUI?style=social)](https://github.com/morganstanley/ComposeUI)


ComposeUI is a A .NET Core based general UI Container and Unified UI and App host which enables the hosting of Web and desktop content.

Our goal is to fill the feature gaps with respect to UI components, layout management, and subpar native hosting in other industry container solutions by providing a standard container as an open desktop platform. It is a hybrid solution that meets the needs of a diverse application catalog as well as a compelling opensource alternative.

It supports desktop and web applications in order to provide an evergreen alternative to Electron, OpenFin and similar by the use of WebView2.

## Development Setup

### Lerna

The javascript dependencies are managed by a lerna monorepo.

### Run scripts

In the root folder
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
