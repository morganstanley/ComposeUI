/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/


using System.Collections.Immutable;

namespace MorganStanley.ComposeUI.ModuleLoader;

/// <summary>
/// Represents a collection of environment variables as key-value pairs.
/// </summary>
public sealed class EnvironmentVariables
{
    /// <summary>
    /// Gets the collection of environment variables.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> Variables { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariables"/> class with the specified variables.
    /// </summary>
    /// <param name="variables">The environment variables to include in the collection.</param>
    public EnvironmentVariables(IEnumerable<KeyValuePair<string, string>> variables)
    {
        Variables = variables.ToImmutableArray();
    }
}
