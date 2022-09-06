<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

ModulesDockingPrototype
================

This is a prototype showing how to create a View. It is preferred that the views are created as DataTemplates with properties on their elements bound to their corresponding view models, since this will help avoinding code-behind anti-pattern. The clients are however free to create views as they wish.
Views can be created in WPF or in Avalonia (multiplatform version of WPF). Note that WPF views will only run on Windows.
Both Avalonia and WPF views can host HTML/Javascript applications within themselves. 

