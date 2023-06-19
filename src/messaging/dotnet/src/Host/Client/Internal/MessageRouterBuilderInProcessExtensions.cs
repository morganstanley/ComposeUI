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

using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Client.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds extension methods for configuring an in-process Message Router client.
/// </summary>
public static class MessageRouterBuilderInProcessExtensions
{
    /// <summary>
    /// Configures the Message Router to connect to the in-process server.
    /// The server must be set up using <see cref="ServiceCollectionMessageRouterServerExtensions.AddMessageRouterServer"/>.
    /// </summary>
    /// <returns></returns>
    public static MessageRouterBuilder UseServer(this MessageRouterBuilder builder)
    {
        builder.ServiceCollection.AddSingleton<IConnectionFactory, InProcessConnectionFactory>();

        return builder;
    }
}