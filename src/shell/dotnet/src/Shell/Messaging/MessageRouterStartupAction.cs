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

internal sealed class MessageRouterStartupAction : IStartupAction
{
    private readonly IMessageRouterWebSocketServer? _webSocketServer;
    private static Dictionary<string, IModuleTypeMessageRouterConfigurator> _configurators;

    public MessageRouterStartupAction(IMessageRouterWebSocketServer? webSocketServer = null)
    {
        _webSocketServer = webSocketServer;
        _configurators = new()
        {
            { ModuleType.Web, new WebModuleMessageRouterConfigurator() },
            { ModuleType.Native, new NativeModuleMessageRouterConfigurator() }
        };
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="startupContext"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public Task InvokeAsync(StartupContext startupContext, Func<Task> next)
    {
        if (_webSocketServer == null)
        {
            return next();
        }

        if (_configurators.TryGetValue(startupContext.ModuleInstance.Manifest.ModuleType, out var configurator))
        {
            configurator.Configure(startupContext, _webSocketServer);
        }

        return next();
    }
}
