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

using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;
internal class Fdc3ShutdownAction : IShutdownAction
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Fdc3ShutdownAction> _logger;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    public Fdc3ShutdownAction(
        IServiceProvider serviceProvider,
        ILogger<Fdc3ShutdownAction>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? NullLogger<Fdc3ShutdownAction>.Instance;
    }

    public async Task InvokeAsync(ShutdownContext shutDownContext, Func<Task> next, TimeSpan timeout = default)
    {
        if (shutDownContext.ModuleInstance.Manifest.ModuleType == ModuleType.Web)
        {
            var desktopAgent = _serviceProvider.GetRequiredService<IFdc3DesktopAgentBridge>();

            var fdc3InstanceId = shutDownContext.ModuleInstance.GetProperties<Fdc3StartupProperties>().FirstOrDefault()?.InstanceId;

            if (fdc3InstanceId != null)
            {
                if (timeout == default)
                {
                    timeout = DefaultTimeout;
                }

                using var cts = new CancellationTokenSource(timeout);

                try
                {   
                    await desktopAgent.CloseModule(fdc3InstanceId, cts.Token);
                }
                catch(OperationCanceledException)
                {
                    _logger.LogError("Timeout: Couldn't finish cleanup task in time. Failed to close module.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Clouldn't close module: {ex.Message}");
                    throw;
                }
            }
        }

        await next();
    }
}
