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
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;
internal class Fdc3ShutdownAction : IShutdownAction
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<Fdc3ShutdownAction> _logger;
    private TaskCompletionSource<bool> _cleanupTcs;
    private const int Timeout = 2;

    public Fdc3ShutdownAction(
        IServiceProvider serviceProvider,
        IMessageRouter messageRouter,
        ILogger<Fdc3ShutdownAction>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _messageRouter = messageRouter;
        _logger = logger ?? NullLogger<Fdc3ShutdownAction>.Instance;
    }

    public async Task InvokeAsync(ShutdownContext shutDownContext, Func<Task> next)
    {
        if (shutDownContext.ModuleInstance.Manifest.ModuleType == ModuleType.Web)
        {
            var desktopAgent = _serviceProvider.GetRequiredService<IFdc3DesktopAgentBridge>();

            var fdc3InstanceId = GetInstanceId(shutDownContext.ModuleInstance);

            if (fdc3InstanceId != null)
            {
                var privateChannels = desktopAgent.GetPrivateChannelsByInstanceId(fdc3InstanceId);

                if (privateChannels != null)
                {
                    foreach (var channel in privateChannels)
                    {
                        var topic = $@"ComposeUI/fdc3/v2.0/privateChannels/{channel.Id}/events";
                        var observer = CreateObserver();
                        var subscription = await _messageRouter.SubscribeAsync(topic, observer);

                        _cleanupTcs = new TaskCompletionSource<bool>();
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
                        cts.Token.Register(() => _cleanupTcs.TrySetCanceled());

                        await _messageRouter.PublishAsync(topic, "{\"event\": \"disconnected\"}");

                        try
                        {
                            await _cleanupTcs.Task;
                        }
                        catch (TaskCanceledException)
                        {
                            _logger.LogError("Timeout: Clouldn't finish cleanup task in time.");
                        }

                        await subscription.DisposeAsync();
                    }
                }
            }
        }

        await next();
    }

    private static string? GetInstanceId(IModuleInstance moduleInstance)
    {
        if (moduleInstance == null)
        {
            return null;
        }

        return moduleInstance.StartRequest.Parameters
            .Where(p => p.Key == "Fdc3InstanceId")
            .Select(p => p.Value).FirstOrDefault();
    }

    private IAsyncObserver<string> CreateObserver()
    {
        return AsyncObserver.Create<string>(
        async message =>
        {
            if (message.Contains("disconnected"))
            {
                _cleanupTcs?.TrySetResult(true);
                await Task.CompletedTask;
            }
        },
        async error =>
        {
            _logger.LogError(error.Message);
            await Task.CompletedTask;
        },
        async () =>
        {
            await Task.CompletedTask;
        });
    }
}
