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

using System.Diagnostics.CodeAnalysis;

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Defines the contract for a module manifest, which describes the metadata and configuration of a module.
/// </summary>
public interface IModuleManifest
{
    /// <summary>
    /// Gets the unique identifier of the module.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the module.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the module (e.g., "Web", "Native", etc.).
    /// </summary>
    public string ModuleType { get; }

    /// <summary>
    /// Gets the tags associated with the module, used for grouping and filtering.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Gets additional properties that can be used to extend the manifest, such as colors, fonts, etc.
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; }
}

/// <summary>
/// Defines the contract for a module manifest with strongly-typed details.
/// </summary>
/// <typeparam name="TDetails">The type of the details object.</typeparam>
public interface IModuleManifest<out TDetails> : IModuleManifest
{
    /// <summary>
    /// Gets the strongly-typed details associated with the module manifest.
    /// </summary>
    public TDetails Details { get; }
}

/// <summary>
/// Provides extension methods for working with <see cref="IModuleManifest"/> instances.
/// </summary>
public static class ModuleManifestExtensions
{
    /// <summary>
    /// Attempts to retrieve the strongly-typed details from a module manifest.
    /// </summary>
    /// <typeparam name="TDetails">The type of the details.</typeparam>
    /// <param name="manifest">The module manifest instance.</param>
    /// <param name="details">
    /// When this method returns, contains the details object if the operation succeeds, or <c>default</c> otherwise.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="manifest"/> implements <see cref="IModuleManifest{TDetails}"/>; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetDetails<TDetails>(this IModuleManifest manifest, [NotNullWhen(true)] out TDetails details)
    {
        if (manifest is IModuleManifest<TDetails> manifestDetails)
        {
            details = manifestDetails.Details!;

            return true;
        }

        details = default!; 

        return false;
    }
}