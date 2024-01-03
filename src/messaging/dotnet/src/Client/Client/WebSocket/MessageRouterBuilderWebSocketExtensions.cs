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

using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.WebSocket;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Static utilities for configuring WebSocket connections.
/// </summary>
public static class MessageRouterBuilderWebSocketExtensions
{
    /// <summary>
    ///     Configures the Message Router client to use WebSocket protocol.
    /// </summary>
    /// <returns></returns>
    public static MessageRouterBuilder UseWebSocket(
        this MessageRouterBuilder builder,
        MessageRouterWebSocketOptions options)
    {
        builder.ServiceCollection.AddSingleton<IOptions<MessageRouterWebSocketOptions>>(options);
        builder.ServiceCollection.AddSingleton<IConnectionFactory, WebSocketConnectionFactory>();
        return builder;
    }

    public static MessageRouterBuilder UseWebSocketFromEnvironment(this MessageRouterBuilder builder)
    {
        var messageRouterUri = Environment.GetEnvironmentVariable(WebSocketEnvironmentVariableNames.Uri);
        if (string.IsNullOrEmpty(messageRouterUri))
        {
            throw new Exception($"{WebSocketEnvironmentVariableNames.Uri} environment variable is not set or empty");
        }

        var opt = new MessageRouterWebSocketOptions { Uri = new Uri(messageRouterUri) };
        return UseWebSocket(builder, opt);
    }
}