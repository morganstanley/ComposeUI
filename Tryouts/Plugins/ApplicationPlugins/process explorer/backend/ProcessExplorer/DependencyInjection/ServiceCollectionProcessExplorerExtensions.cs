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
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Processes;
using ProcessExplorer.Core;
using ProcessExplorer.Core.Processes;

namespace ProcessExplorer.Server.DependencyInjection;

public static class ServiceCollectionProcessExplorerExtensions
{
    public static IServiceCollection AddProcessExplorerAggregator(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<IProcessInfoAggregator, ProcessInfoAggregator>();
        return serviceCollection;
    }

    public static IServiceCollection AddProcessMonitorWindows(
        this IServiceCollection serviceCollection)
    {
        //serviceCollection.TryAddSingleton<IProcessMonitor, ProcessMonitor>();
        serviceCollection.TryAddSingleton<ProcessInfoManager, WindowsProcessInfoManager>();

        return serviceCollection;
    }
}
