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

using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.LocalCollector.Logging;

namespace MorganStanley.ComposeUI.ProcessExplorer.LocalCollector;

public static class InformationCollectorHelper
{
    public static IEnumerable<ModuleInfo> GetModulesFromAssembly()
    {
        var modules = new List<ModuleInfo>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var loadedModules = assembly.GetLoadedModules();

            foreach (var module in loadedModules)
            {
                modules.Add(ModuleInfo.FromModule(assembly, module));
            }
        }

        return modules;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariablesFromAssembly(ILogger? logger = null)
    {
        var environmentVariables = new Dictionary<string, string>();

        foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
        {
            try
            {
                var itemValue = item.Value?.ToString();
                var itemKey = item.Key.ToString();
                if (itemKey == null || itemValue == null)
                {
                    logger?.EnvironmentVariableParingErrorDebug(itemKey, itemValue);
                    continue;
                }

                if (!environmentVariables.TryAdd(itemKey, itemValue))
                    logger?.EnvironmentVariableAddErrorDebug(itemKey, itemValue);
            }
            catch { }
        }

        return environmentVariables;
    }

    public static IEnumerable<RegistrationInfo> GetRegistrations(IServiceCollection serviceCollection)
    {
        var registrations = new List<RegistrationInfo>();

        foreach (var service in serviceCollection)
        {
            var registration = new RegistrationInfo
            {
                ServiceType = service.ServiceType.Name ?? string.Empty,
                ImplementationType = service.ImplementationType?.Name ?? string.Empty,
                LifeTime = service.Lifetime.ToString() ?? string.Empty,
                };

            registrations.Add(registration);
        }

        return registrations;
    }
}
