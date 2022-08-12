<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

ThemeChangerPrototype
=====================

Important Note on Build: The main application should not depend on the plugins (which are loaded dynamically) so you have to build the plugins
separately from the main applicaiton by right clicking on Plugins folder within Solution Explorer and choosing Rebuild option.

Important Note on Running the Applicaiton:
Before you run the applicaiton, please make sure that the folders
Services
ViewModelPlugins
ViewPlugins

exist under <ThemeChangerPrototype>\bin\Debug\net6.0\Plugins

Otherwise, the software will throw an exception complaining about those missing folders. 