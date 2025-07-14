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

/// <summary>
/// Represents a collection of modules that can be started in the scope of the application.
/// </summary>
/// <exception cref="ModuleNotFoundException">The requested module was not found in the catalog</exception>
public interface IModuleCatalog
{
    /// <summary>
    /// Gets a module's manifest by its module ID.
    /// </summary>
    /// <param name="moduleId"></param>
    /// <returns></returns>
    public Task<IModuleManifest> GetManifest(string moduleId);

    /// <summary>
    /// Gets the IDs of the modules in the catalog.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<string>> GetModuleIds();

    /// <summary>
    /// Gets the manifests of all modules in the catalog.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<IModuleManifest>> GetAllManifests();
}
