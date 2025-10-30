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

using System.Threading.Channels;
using MorganStanley.ComposeUI.Messaging.MessageRouterAdapter.Internal;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Messaging.MessageRouterAdapter;

internal class MessageRouterAdapterStartupAction : IStartupAction
{
    private static readonly Dictionary<string, StartupModuleHandler> Handlers = new()
    {
        { ModuleType.Web, new WebStartupModuleHandler() },
        { ModuleType.Native, new NativeStartupModuleHandler() }
    };

    public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
    {
        if (Handlers.TryGetValue(startupContext.ModuleInstance.Manifest.ModuleType, out var handler))
        {
            await handler.HandleAsync(startupContext);
        }

        await next.Invoke();
    }
}