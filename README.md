<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# ComposeUI

![Lifecycle Incubating](https://badgen.net/badge/Lifecycle/Incubating/yellow) [![Build Status](https://github.com/MorganStanley/ComposeUI/actions/workflows/continuous-integration.yml/badge.svg?event=push)](https://github.com/MorganStanley/ComposeUI/actions/workflows/continuous-integration.yml)
[![NPM](https://img.shields.io/npm/v/@morgan-stanley/composeui-node-launcher)](https://www.npmjs.com/package/@morgan-stanley/composeui-node-launcher)
[![NuGet](https://img.shields.io/nuget/v/MorganStanley.ComposeUI.svg?style=flat)](https://www.nuget.org/packages/MorganStanley.ComposeUI/)
[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/morganstanley/ComposeUI/badge)](https://securityscorecards.dev/viewer/?uri=github.com/morganstanley/ComposeUI)
[![codecov](https://codecov.io/gh/MorganStanley/ComposeUI/branch/main/graph/badge.svg)](https://codecov.io/gh/MorganStanley/ComposeUI)
[![GitHub Repo stars](https://img.shields.io/github/stars/morganstanley/ComposeUI?style=social)](https://github.com/morganstanley/ComposeUI)

ComposeUI is a A .NET Core based general UI Container and Unified UI and App host which enables the hosting of Web and desktop content.

Our goal is to fill the feature gaps with respect to UI components, layout management, and subpar native hosting in other industry container solutions by providing a standard container as an open desktop platform. It is a hybrid solution that meets the needs of a diverse application catalog as well as a compelling opensource alternative.

It supports desktop and web applications in order to provide an evergreen alternative to Electron, OpenFin and similar by the use of WebView2.

# FDC3 2.0 Compliance

<p align="center">
<a href="https://fdc3.finos.org/docs/2.0/fdc3-intro"><img src=site/images/certified-2.0.png width="25%" height="25%"/></a>
</p>

As [announced](https://www.finos.org/press/fdc3-2.0-blackrock-morgan-stanley-lead-charge) at OSSF NY '24 ComposeUI has successfully achieved the **FDC3 2.0** Conformance on **9/30/2024** certified by FINOS.
FDC3 is an open standard for applications on financial desktop to interoperate and exchange data with each other. Learn More https://fdc3.finos.org/

We've released the FDC3 2.0 compliant container in **v0.1.0-alpha.6** with IPC related bug fixes. The packages can be installed from npmjs.com and nuget.org

# Releases

## @morgan-stanley/composeui-node-launcher

[![npm](https://img.shields.io/npm/v/@morgan-stanley/composeui-node-launcher)](https://www.npmjs.com/package/@morgan-stanley/composeui-node-launcher)

# Development Setup

## Prerequisites
* Node.js 22
* .NET 8 (SDK 8.0.x, Desktop Runtime 8.0.x)
* Visual Studio 2022 Community Edition

## Building the ComposeUI Container


Clone the main repository:
```
git clone https://github.com/morganstanley/ComposeUI.git
```

### From Visual Studio IDE

Build the "Shell.sln" solution.

By doing this the build is kicked off to all the other solutions it depends on.

### From Terminal

To build the Nuget and NPM packages without an IDE open Powershell in the ComposeUI folder.

1. Build javascript (with Lerna)

```
C:\projects\ComposeUI> .\build\lerna-build.ps1
```

If you only want to build a specific package see [how to build packages with Lerna](#building-the-javascript-dependencies-with-lerna)


2. Restore nuget packages:

```
C:\projects\ComposeUI> .\build\dotnet-restore.ps1
```

3. Build .NET solutions:

```
C:\projects\ComposeUI> .\build\dotnet-build.ps1
```

Now the necessary artifacts have been built.

## Building the Examples

### FDC3 Chart and Grid Example

From the ComposeUI folder:

```
.\examples\fdc3-chart-and-grid\serve-chart-and-grid.ps1
```
Now the development servers are running:
* Chart: localhost:8080
* Grid: localhost:4200

### Launching the Shell with the Examples

#### From Visual Studio

Start the "Shell" project.

You can add a new launch setting in the IDE or by adding a new entry to the `$(ProjectRoot))\src\shell\dotnet\Shell\Properties\launchSettings.json` file

### From Terminal

1. It's recommended to add the shell binary to your PATH environment variable so you can use a shorthand:

```
cd .src\shell\dotnet\
```
```
.\add-to-path.ps1
```
2. Launch the FDC3 Example in the ComposeUI Shell:

```
MorganStanley.ComposeUI.Shell --ModuleCatalog:CatalogUrl file:///C:/ComposeUI/src/Shell/dotnet/examples/module-catalog.json --FDC3:AppDirectory:Source C:/ComposeUI/examples/fdc3-appdirectory/apps-with-intents.json
```

### Serving web application

[See](#fdc3-chart-and-grid-example)
 

### Running the Shell
1. Open the Shell Solution
2. Choose "Shell" as the startup project
3. Run




## Building the JavaScript Dependencies with Lerna

The javascript dependencies are managed by a lerna monorepo. To build them separately follow the steps below.

### Run scripts

In the root folder

```
npm i
```

Build all modules:

```
npx lerna run build --stream
```

Test all modules:

```
npx lerna run test --stream
```

(If you don't want a detailed log, you can execute these without --stream)

Building a specific module:

```
npx lerna run build --stream --scope=@morgan-stanley/composeui-messaging-client
```

List all modules in the workspace

```
npx lerna list
```

### Docs

For more information check the [documentation](https://lerna.js.org/docs/api-reference/commands).

## How to Run the FDC3 Conformance test Locally

1. Clone the [FDC3 Conformance Framework](https://github.com/finos/FDC3-conformance-framework) and build it: 
2. Add the following to the `$((ProjectRoot))\src\shell\dotnet\Shell\Properties\launchSettings.json` file:

```
    "FDC3-Local": {
      "commandName": "Project",
      "commandLineArgs": "--ModuleCatalog:CatalogUrl \"file:///$(ProjectDir)..\\..\\examples\\module-catalog.json\" --FDC3:AppDirectory:Source Path-To-Local-Conformance-Framework-Root\\FDC3-conformance-framework\\directories\\local-conformance-2_0.v2.json"
    }
```
# Building the Experimental Artifacts

The following steps are for building the experimental artifacts and shell for ComposeUI

Clone the main repository:

```
git clone https://github.com/morganstanley/ComposeUI.git
```

## Terminal

### Building Nuget and NPM packages

Open Powershell in the ComposeUI folder.

Restore nuget packages:

```
PS C:\projects\ComposeUI> .\build\dotnet-restore.ps1
```

Build .NET solutions:

```
PS C:\projects\ComposeUI> .\build\dotnet-build.ps1
```

Build javascript (with Lerna)

```
PS C:\projects\ComposeUI> .\build\lerna-build.ps1
```

Now the necessary artifacts have been built.

### Building the Examples

#### FDC3 Chart and Grid Example

From the ComposeUI folder:

```
.\examples\fdc3-chart-and-grid\serve-chart-and-grid.ps1
```

Now the development servers are running:

- Chart: localhost:8080
- Grid: localhost:4200

### Launching the Shell with the Examples

1. It's recommended to add the shell binary to your PATH environment variable so you can use a shorthand:

```
cd .src\shell\dotnet\
```

```
.\add-to-path.ps1
```

2. Launch the FDC3 Example in the ComposeUI Shell:

```
MorganStanley.ComposeUI.Shell --ModuleCatalog:CatalogUrl file:///C:/ComposeUI/src/Shell/dotnet/examples/module-catalog.json --FDC3:AppDirectory:Source C:/ComposeUI/examples/fdc3-appdirectory/apps-with-intents.json
```

## Visual Studio

Similar steps can be taken in Visual Studio to have the same affect.
[See Prerequisites](##Prerequisites)

### Building Solutions

#### For the FDC3 Samples

Build the "Shell" solution. This will kick off the build of the other solutions that the shell depends on.

### Serving web application

[See FDC3 Chart and Grid Example](####FDC3-Chart-and-Grid-Example)

### Running the Shell

1. Open the Shell Solution
2. Choose "Shell" as the startup project
3. Run
