﻿// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

namespace MorganStanley.ComposeUI.ModuleLoader;

public class ModuleStartupProperties
{
    /// <summary>
    /// Initial position of the module.
    /// </summary>
    public InitialModulePosition? InitialModulePosition { get; set; }

    /// <summary>
    /// Width of the opening floating window of the started app.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Height of the opening floating window of the started app.
    /// </summary>
    public double? Height { get; set; }

    /// <summary>
    /// Initial coordinate positions of the window if it is opened as floating window.
    /// </summary>
    public Coordinates? Coordinates { get; set; }
}