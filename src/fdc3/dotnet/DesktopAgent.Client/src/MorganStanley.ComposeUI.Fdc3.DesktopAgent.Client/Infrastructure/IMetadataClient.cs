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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;

/// <summary>
/// Provides methods for retrieving FDC3 implementation and application metadata.
/// </summary>
internal interface IMetadataClient
{
    /// <summary>
    /// Gets metadata information about the FDC3 implementation.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{IImplementationMetadata}"/> representing the asynchronous operation to retrieve implementation metadata.
    /// </returns>
    public ValueTask<IImplementationMetadata> GetInfoAsync();

    /// <summary>
    /// Gets metadata for a specific FDC3 application.
    /// </summary>
    /// <param name="appIdentifier">The identifier of the application.</param>
    /// <returns>
    /// A <see cref="ValueTask{IAppMetadata}"/> representing the asynchronous operation to retrieve application metadata.
    /// </returns>
    public ValueTask<IAppMetadata> GetAppMetadataAsync(IAppIdentifier appIdentifier);

    /// <summary>
    /// Finds all instances of a specific FDC3 application.
    /// </summary>
    /// <param name="appIdentifier"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<IAppIdentifier>> FindInstancesAsync(IAppIdentifier appIdentifier);
}
