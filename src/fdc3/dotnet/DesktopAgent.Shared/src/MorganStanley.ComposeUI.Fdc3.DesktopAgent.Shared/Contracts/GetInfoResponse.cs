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
using ImplementationMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.ImplementationMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Response for the `fdc3.getInfo()` method.
/// </summary>
internal sealed class GetInfoResponse
{
    /// <summary>
    /// Result of the `fdc3.getInfo` query containing information about the desktop agent provider.
    /// </summary>
    public IImplementationMetadata? ImplementationMetadata { get; set; }

    /// <summary>
    /// Error, if something went wrong during the execution of the query.
    /// </summary>
    public string? Error { get; set; }

    public static GetInfoResponse Failure(string error) => new() { Error = error };

    public static GetInfoResponse Success(ImplementationMetadata implementationMetadata) =>
        new() { ImplementationMetadata = implementationMetadata };
}