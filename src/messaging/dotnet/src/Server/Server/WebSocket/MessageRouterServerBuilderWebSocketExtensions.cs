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

using Microsoft.Extensions.Hosting;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MessageRouterServerBuilderWebSocketExtensions
{
    public static MessageRouterServerBuilder UseWebSockets(
        this MessageRouterServerBuilder builder,
        Action<MessageRouterWebSocketServerOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            builder.ServiceCollection.Configure<MessageRouterWebSocketServerOptions>(configureOptions);
        }

        builder.ServiceCollection.AddSingleton<WebSocketListenerService>();
        builder.ServiceCollection.AddSingleton<IHostedService>(provider => provider.GetRequiredService<WebSocketListenerService>());
        builder.ServiceCollection.AddSingleton<IMessageRouterWebSocketServer>(provider => provider.GetRequiredService<WebSocketListenerService>());

        return builder;
    }
}
