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
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Processes;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core;
using ProcessExplorer.Core.DependencyInjection;
using ProcessExplorer.Core.Processes;
using ProcessExplorer.Core.Subsystems;

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
#pragma warning disable CA1416 // Validate platform compatibility
        serviceCollection.TryAddSingleton<IProcessInfoManager, WindowsProcessInfoManager>();
#pragma warning restore CA1416 // Validate platform compatibility
        return serviceCollection;
    }

    public static IServiceCollection ConfigureSubsystemLauncher<LaunchRequestType, StopRequestType>(
        this IServiceCollection serviceCollection,
        Action<LaunchRequestType>? launchRequest,
        Action<StopRequestType>? stopRequest,
        Func<Guid, string, LaunchRequestType>? launcRequestCtor,
        Func<Guid, StopRequestType>? stopRequestCtor)
    {
        serviceCollection.TryAddSingleton<ISubsystemLauncher, SubsystemLauncher<LaunchRequestType, StopRequestType>>();

        serviceCollection.Configure<SubsystemLauncherOptions<LaunchRequestType, StopRequestType>>(op =>
        {
            op.LaunchRequest = launchRequest;
            op.StopRequest = stopRequest;
            op.CreateLaunchRequest = launcRequestCtor;
            op.CreateStopRequest = stopRequestCtor;
        });

        return serviceCollection;
    }
}
