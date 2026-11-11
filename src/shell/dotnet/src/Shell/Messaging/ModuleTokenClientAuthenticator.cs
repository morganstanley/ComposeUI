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
using MorganStanley.ComposeUI.Messaging.Server;

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// Authenticates message router clients using the tokens issued by <see cref="IModuleTokenStore"/>.
/// </summary>
internal sealed class ModuleTokenClientAuthenticator : IClientAuthenticator
{
    public ModuleTokenClientAuthenticator(IModuleTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public ValueTask<ClaimsPrincipal> Authenticate(string clientId, string? accessToken)
    {
        if (_tokenStore.IsHostToken(accessToken))
        {
            return new ValueTask<ClaimsPrincipal>(
                CreatePrincipal(
                    "ComposeUI.Shell",
                    new Claim(MessageRouterClaimTypes.IdentityType, MessageRouterClaimTypes.IdentityTypes.Host)));
        }

        if (_tokenStore.TryValidate(accessToken, out var identity))
        {
            return new ValueTask<ClaimsPrincipal>(
                CreatePrincipal(
                    identity.ModuleId,
                    new Claim(MessageRouterClaimTypes.IdentityType, MessageRouterClaimTypes.IdentityTypes.Module),
                    new Claim(MessageRouterClaimTypes.ModuleInstanceId, identity.ModuleInstanceId.ToString()),
                    new Claim(MessageRouterClaimTypes.ModuleId, identity.ModuleId)));
        }

        throw new InvalidOperationException("The provided access token is invalid");
    }

    private readonly IModuleTokenStore _tokenStore;

    private static ClaimsPrincipal CreatePrincipal(string name, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(
            claims.Append(new Claim(ClaimTypes.Name, name)),
            MessageRouterClaimTypes.AuthenticationType);

        return new ClaimsPrincipal(identity);
    }
}
