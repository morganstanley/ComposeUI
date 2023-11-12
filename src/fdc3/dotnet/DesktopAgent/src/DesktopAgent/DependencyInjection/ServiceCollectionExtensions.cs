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

using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.ComposeUI.Shell.Fdc3;

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
        serviceCollection.AddTransient<IStartupAction, Fdc3StartupAction>();

        return serviceCollection;
    }
}
