<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

# ModulesPrototype

This prototype demos starting and stopping modules based on a basic manifest file.

## Using the prototype
### Build requirements
Aside from the base dotnet stack, the demo makes uses of node.js to compile web based projects.

### Launching the application
Start the ModulesPrototype project from Visual Studio, or directly from the build folder in order to start the protype application.

In the default configuration, the console shell application will start 4 different modules:
 - MessageRouter for providing communications between "Compose-ready" applications/modules
 - DataService as an example for a background data aggregator module providing data for other compose modules
 - WPFDataGrid as a sample application with WPF UI, publishing data via a MessageRouter topic others can subscibe to
 - Chart as a sample locally hosted web application reacting on data arriving via the MessageRouter, and using the router to obtain further data

The console shell will restart applications that exit without the shell requesting it (see [Exiting the application](#exiting-the-application))

### Triing different configurations
The prototype starts up all entries from the _manifet.json_ file in order. It is possible to modify the file to try other configurations.

## Exiting the application
The prototype may launch several windows, but will always have a main window that displays detected lifecycle events (module started, stopped...). Pressing the enter key in this window will signal all modules to exit. The exit requests will be sent in reverse order compared to starting order.