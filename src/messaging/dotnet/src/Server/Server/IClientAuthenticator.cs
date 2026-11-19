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

using System.Security.Claims;

namespace MorganStanley.ComposeUI.Messaging.Server;

/// <summary>
/// Authenticates client connections and establishes their identity.
/// </summary>
/// <remarks>
/// Unlike <see cref="IAccessTokenValidator"/>, which can only accept or reject a connection,
/// an authenticator also tells the server who the client is. The returned principal is stored
/// for the lifetime of the connection.
/// If both are registered, the validator runs first and both must accept the connection.
/// </remarks>
public interface IClientAuthenticator
{
    /// <summary>
    /// Authenticates a client connection. The method should throw an exception if the token is invalid or missing.
    /// </summary>
    /// <param name="clientId">The identifier of the client connection.</param>
    /// <param name="accessToken">The access token provided by the client.</param>
    /// <returns>The identity of the client.</returns>
    public ValueTask<ClaimsPrincipal> Authenticate(string clientId, string? accessToken);
}
