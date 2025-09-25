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

using Finos.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Response from the service which provides the ResolverUI's functionality.
/// </summary>
public class ResolverUIResponse
{
    /// <summary>
    /// The chosen app to send the raised intent to handle.
    /// </summary>
    public IAppMetadata? AppMetadata { get; set; }

    /// <summary>
    /// Any error message that happened during execution, either from <see cref="ResolveError"/>.
    /// </summary>
    public string? Error { get; set; }
}