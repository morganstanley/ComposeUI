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

// ReSharper disable once CheckNamespace

using ComposeUI.Messaging.Client;
using ComposeUI.Messaging.Client.Startup;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Static extensions to add the Message Router client to a service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the <see cref="IMessageRouter" /> and related types to the service collection,
    ///     using the provided configuration callback.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageRouter(
        this IServiceCollection serviceCollection,
        Action<MessageRouterBuilder> builderAction)
    {
        serviceCollection.AddTransient<IMessageRouter, MessageRouterClient>();
        var builder = new MessageRouterBuilder(serviceCollection);
        builderAction(builder);
        return serviceCollection;
    }
}