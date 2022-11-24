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

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server;

/// <summary>
/// Validates access tokens provided by client connections.
/// </summary>
public interface IAccessTokenValidator
{
    /// <summary>
    /// Validates an access token. The method should throw an exception if the token is invalid or missing.
    /// </summary>
    /// <param name="clientId">The identifier of the client connection.</param>
    /// <param name="accessToken">The access token provided by the client.</param>
    /// <returns></returns>
    ValueTask Validate(string clientId, string? accessToken);
}
