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

public interface IModuleManifest
{
    string Id { get; }

    string Name { get; }

    string ModuleType { get; }
}

public interface IModuleManifest<out TDetails> : IModuleManifest
{
    TDetails Details { get; }
}

public static class ModuleManifestExtensions
{
    /// <summary>
    /// Shorthand for querying a module manifest for a specific details type.
    /// </summary>
    /// <typeparam name="TDetails">The type of the details</typeparam>
    /// <param name="manifest">The module manifest</param>
    /// <param name="details">The variable receiving the details object when the operation succeeds (or <c>default</c> otherwise).</param>
    /// <returns>
    /// True, if <paramref name="manifest"/> implements <see cref="IModuleManifest{TDetails}"/>.
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