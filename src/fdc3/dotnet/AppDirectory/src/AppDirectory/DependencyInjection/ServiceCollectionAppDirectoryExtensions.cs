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

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global

using Microsoft.Extensions.DependencyInjection.Extensions;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.ModuleLoader;
using MorganStanley.Fdc3.AppDirectory;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionAppDirectoryExtensions
{
    public static IServiceCollection AddFdc3AppDirectory(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<IAppDirectory, AppDirectory>();
        serviceCollection.TryAddSingleton<IModuleCatalog, Fdc3ModuleCatalog>();
        return serviceCollection;
    }

    public static IServiceCollection AddFdc3AppDirectory(
        this IServiceCollection serviceCollection,
        Action<AppDirectoryOptions> configureOptions)
    {
        serviceCollection.AddFdc3AppDirectory();
        serviceCollection.AddOptions().Configure(configureOptions);        

        return serviceCollection;
    }
}