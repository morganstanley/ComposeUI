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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class Fdc3DesktopAgentBuilderExtensions
{
    public static Fdc3DesktopAgentBuilder UseMessageRouter(
        this Fdc3DesktopAgentBuilder builder,
        Action<Fdc3DesktopAgentOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            builder.ServiceCollection.Configure<Fdc3DesktopAgentOptions>(configureOptions);
        }

        builder.ServiceCollection.AddSingleton<IResolverUICommunicator, ResolverUIMessageRouterCommunicator>();
        builder.ServiceCollection.AddHostedService<Fdc3DesktopAgentMessageRouterService>();

        return builder;
    }
}
