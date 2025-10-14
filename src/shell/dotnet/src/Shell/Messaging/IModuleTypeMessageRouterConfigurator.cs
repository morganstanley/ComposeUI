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

using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// Defines a contract for configuring the message router for a specific module type.
/// </summary>
public interface IModuleTypeMessageRouterConfigurator
{
    /// <summary>
    /// Configures the message router for the given startup context.
    /// </summary>
    /// <param name="startupContext">The startup context.</param>
    /// <param name="webSocketServer">The WebSocket server instance.</param>
    public void Configure(StartupContext startupContext, IMessageRouterWebSocketServer webSocketServer);
}
