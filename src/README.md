<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# src
=========

src folder contains

1. Core folder containing non-visual multiplatform code that can be re-used between various projects e.g. between various prototypes and plugins. 
2. Visuals folder contains Visual functionality re-usuable between various prototypes and plugins, but not necessarily multiplatform. Its Avalonia folder contains Avalonia functionality (which is multiplatform), but in the future we might add also e.g. a WPF folder which will contain window only WPF re-usable functionality.
3. Plugins folder contains either application plugins (executables separate from the shell that can be displayed by the shell) or dynamically loaded plugins. Main and prototype projects do not directly depend on the dynamically loaded plugins - instead they load them dynamically and deal with their functionality via interfaces. Those interfaces allowing the dynamically loaded plugins to interact with each other, with the shell and other processes are defined within Abstractions project of the Core. 
