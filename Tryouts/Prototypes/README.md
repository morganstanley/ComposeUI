<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

Prototypes
=========

Prototypes folder contains prototypes folder with visual and non-visual
prototypes for various features (check Prototype Driven Development or PDD).
The common first step for a feature development, should be to copy an older protype project and rename it to mention the
new feature you are about to start developing, rename also the solution, project and the namespaces. Then, build the new feature
initially as the prototype. And finally move the features re-usable code into the re-usable projects.

Important note about Building the Prototypes:
Main prototype solution usually does not depend on the plugins, you might need to plugins separately by right-clicking on the Plugins
folder within Visual Studio's solution explorer and choosing Rebuild menu item. 