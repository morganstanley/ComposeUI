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

using Microsoft.Extensions.DependencyInjection;

namespace MorganStanley.ComposeUI.Messaging.Server.ModuleAccessTokens;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything needed to give each module instance its own message router access token:
    /// <list type="bullet">
    /// <item>an <see cref="IModuleTokenStore"/> that issues, validates and revokes the tokens,</item>
    /// <item>an <see cref="IClientAuthenticator"/> that turns a token into the identity of a client,
    /// which the message router server picks up when it is registered,</item>
    /// <item>a hosted service that revokes the token of a module instance when the module stops.</item>
    /// </list>
    /// An <see cref="ModuleLoader.IModuleLoader"/> must be registered for the revocation to work.
    /// Call this method only once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>
    /// The token store. The host's own message router client can connect with its
    /// <see cref="IModuleTokenStore.HostToken"/>, and startup actions can issue tokens to modules with it.
    /// </returns>
    public static IModuleTokenStore AddModuleAccessTokens(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var tokenStore = new ModuleTokenStore();

        services.AddSingleton<IModuleTokenStore>(tokenStore);
        services.AddSingleton<IClientAuthenticator>(new ModuleTokenClientAuthenticator(tokenStore));
        services.AddHostedService<ModuleTokenRevocationService>();

        return tokenStore;
    }
}
