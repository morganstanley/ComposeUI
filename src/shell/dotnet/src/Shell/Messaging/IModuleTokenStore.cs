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

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// Issues, validates and revokes message router access tokens. Tokens are issued per module instance.
/// </summary>
internal interface IModuleTokenStore
{
    /// <summary>
    /// Gets the access token of the shell host (the in-process message router client and the services built on it).
    /// It is different from every module token and is valid for the lifetime of the store.
    /// </summary>
    string HostToken { get; }

    /// <summary>
    /// Checks if the access token is the shell host's token.
    /// </summary>
    /// <param name="token">The access token provided by the client.</param>
    bool IsHostToken(string? token);

    /// <summary>
    /// Issues an access token for the specified module instance.
    /// If a token was already issued for the instance (and not revoked), the same token is returned.
    /// </summary>
    /// <param name="moduleInstanceId">The unique ID of the module instance.</param>
    /// <param name="moduleId">The ID of the module (from its manifest).</param>
    /// <returns>The access token.</returns>
    string Issue(Guid moduleInstanceId, string moduleId);

    /// <summary>
    /// Validates an access token.
    /// </summary>
    /// <param name="token">The access token provided by the client.</param>
    /// <param name="identity">When the method returns <c>true</c>, the identity of the module instance the token was issued to.</param>
    /// <returns><c>true</c> if the token is valid, <c>false</c> if it is missing, unknown or revoked.</returns>
    bool TryValidate(string? token, [NotNullWhen(true)] out ModuleIdentity? identity);

    /// <summary>
    /// Revokes the token issued to the specified module instance.
    /// </summary>
    /// <param name="moduleInstanceId">The unique ID of the module instance.</param>
    /// <returns><c>true</c> if a token was revoked, <c>false</c> if the instance had no token.</returns>
    bool Revoke(Guid moduleInstanceId);
}
