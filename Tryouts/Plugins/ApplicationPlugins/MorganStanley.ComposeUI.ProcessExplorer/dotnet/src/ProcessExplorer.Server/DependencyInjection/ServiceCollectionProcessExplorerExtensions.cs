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
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Core.DependencyInjection;
using ProcessExplorer.Server.Server.Infrastructure.Grpc;

namespace ProcessExplorer.Server.DependencyInjection;

public static class ServiceCollectionProcessExplorerExtensions
{
    public static IServiceCollection AddProcessExplorerWindowsServerWithGrpc(
        this IServiceCollection serviceCollection,
        Action<ProcessExplorerBuilder> builderAction)
    {
        serviceCollection.TryAddSingleton<IUiHandler, GrpcUiHandler>();
        serviceCollection.AddProcessMonitorWindows();
        serviceCollection.AddProcessExplorerAggregator();

        var builder = new ProcessExplorerBuilder(serviceCollection);
        builderAction(builder);

        return serviceCollection;
    }
}
