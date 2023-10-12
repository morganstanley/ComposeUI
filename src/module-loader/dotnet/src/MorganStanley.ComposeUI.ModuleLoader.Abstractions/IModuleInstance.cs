// Morgan Stanley makes this available to you under the Apache License,
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
/// Represents a running instance of a module.
/// </summary>
public interface IModuleInstance
{
    /// <summary>
    /// Gets the unique ID of the module instance.
    /// </summary>
    Guid InstanceId { get; }

    /// <summary>
    /// Gets the manifest of the module.
    /// </summary>
    IModuleManifest Manifest { get; }

    /// <summary>
    /// Gets the original <see cref="StartRequest"/> that was used to start the module.
    /// </summary>
    StartRequest StartRequest { get; }

    /// <summary>
    /// Gets the properties of type <typeparamref name="T"/> attached to the module instance.
    /// </summary>
    /// <typeparam name="T">The type of the properties to get</typeparam>
    /// <returns>The collection of properties of the specified type, in the order they were added to the <see cref="StartupContext"/></returns>
    IEnumerable<T> GetProperties<T>();
}