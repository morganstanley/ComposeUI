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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFdc3DesktopAgent(
        this IServiceCollection serviceCollection,
        Action<Fdc3DesktopAgentBuilder>? builderAction = null)
    {
        var builder = new Fdc3DesktopAgentBuilder(serviceCollection);

        if (builderAction != null)
        {
            builderAction(builder);
        }

        serviceCollection.AddSingleton<IHostedService, Fdc3DesktopAgent>();
        return serviceCollection;
    }

    /// <summary>
    /// Checks the configuration, if that contains Fdc3Options part, where the user could set the EnableFdc3 tag to true.
    /// If that tag value is true, it will add the DesktopAgent service to the ServiceCollection, with the given Fdc3Options, that was set in the configuration.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configuration">This should be the IConfigurationSection, which contains the configuration for Fdc3.</param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public static IServiceCollection InjectFdc3BackendServiceIfEnabledFromConfig(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Action<Fdc3DesktopAgentBuilder>? builderAction = null)
    {
        var fdc3Options = configuration.Get<Fdc3Options>();

        //TODO: This should be feature toggle, once we have feature toggles in the future - instead of having `EnableFdc3` inside Fdc3Options.
        if (fdc3Options.EnableFdc3)
        {
            serviceCollection.Configure<Fdc3Options>(configuration);
            serviceCollection.AddFdc3DesktopAgent(builderAction);
        }

        return serviceCollection;
    }
}
