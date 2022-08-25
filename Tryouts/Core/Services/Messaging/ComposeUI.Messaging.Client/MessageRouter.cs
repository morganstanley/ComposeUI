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

using ComposeUI.Messaging.Client.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace ComposeUI.Messaging.Client;

/// <summary>
///     Static utility for creating <see cref="IMessageRouter" /> without a service collection.
/// </summary>
public static class MessageRouter
{
    /// <summary>
    ///     Creates a new instance of <see cref="IMessageRouter" /> and configures it using
    ///     the provided callback.
    /// </summary>
    /// <param name="builderAction"></param>
    /// <returns></returns>
    public static IMessageRouter Create(Action<MessageRouterBuilder> builderAction)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMessageRouter(builderAction);
        return serviceCollection.BuildServiceProvider().GetRequiredService<IMessageRouter>();
    }
}