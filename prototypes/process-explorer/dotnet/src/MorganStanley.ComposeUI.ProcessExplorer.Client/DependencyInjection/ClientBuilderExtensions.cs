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
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Client.Infrastructure;

namespace MorganStanley.ComposeUI.ProcessExplorer.Client.DependencyInjection;

public static class ClientBuilderExtensions
{
    public static ClientBuilder UseGrpc(
        this ClientBuilder builder,
        ClientServiceOptions? options = null)
    {
        if (options != null) builder.ServiceCollection.TryAddSingleton<IOptions<ClientServiceOptions>>(options);
        builder.ServiceCollection.AddGrpc();
        builder.ServiceCollection.TryAddSingleton<ICommunicator, GrpcCommunicator>();
        builder.ServiceCollection.TryAddSingleton<IProcessInfoHandler, ProcessInfoHandler>();

        return builder;
    }
}
