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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;

public class Fdc3DesktopAgentBuilder
{
    public IServiceCollection ServiceCollection { get; }

    public Fdc3DesktopAgentBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    /// <summary>
    /// Method, for configuring `Fdc3Options` from full `IConfiguration` by searching for section `Fdc3Options.Fdc3OptionsName`.
    /// </summary>
    /// <typeparam name="TOptions">Must be Fdc3Options reference type.</typeparam>
    /// <param name="configuration">Full IConfigration passed from the Application.</param>
    /// <returns></returns>
    public Fdc3DesktopAgentBuilder Configure<TOptions>(IConfiguration configuration) 
        where TOptions : class, IOptions<Fdc3Options>
    {
        ServiceCollection.Configure<TOptions>(configuration.GetSection(Fdc3Options.Fdc3OptionsName));

        return this;
    }

    /// <summary>
    /// Extension method, for configuring the `Fdc3Options` by `Action`.
    /// </summary>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public Fdc3DesktopAgentBuilder Configure(Action<Fdc3Options> configureOptions)
    {
        ServiceCollection.AddOptions<Fdc3Options>()
            .Configure(configureOptions);

        return this;
    }
}
