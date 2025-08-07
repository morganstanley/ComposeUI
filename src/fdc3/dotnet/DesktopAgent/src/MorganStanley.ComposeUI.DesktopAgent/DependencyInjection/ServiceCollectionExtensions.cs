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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.Fdc3;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering FDC3 Desktop Agent services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers FDC3 Desktop Agent services and related dependencies into the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the services to.</param>
    /// <param name="builderAction">
    /// An optional action to configure the <see cref="Fdc3DesktopAgentBuilder"/> before services are registered.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddFdc3DesktopAgent(
        this IServiceCollection serviceCollection,
        Action<Fdc3DesktopAgentBuilder>? builderAction = null)
    {
        var builder = new Fdc3DesktopAgentBuilder(serviceCollection);

        if (builderAction != null)
        {
            builderAction(builder);
        }

        builder.ServiceCollection.AddSingleton<IResolverUICommunicator, ResolverUICommunicator>();
        builder.ServiceCollection.AddHostedService<Fdc3DesktopAgentMessagingService>();
        serviceCollection.AddSingleton<IUserChannelSetReader, UserChannelSetReader>();
        serviceCollection.AddSingleton<IFdc3DesktopAgentBridge, Fdc3DesktopAgent>();
        serviceCollection.AddTransient<IStartupAction, Fdc3StartupAction>();
        serviceCollection.AddTransient<IShutdownAction, Fdc3ShutdownAction>();

        return serviceCollection;
    }
}
