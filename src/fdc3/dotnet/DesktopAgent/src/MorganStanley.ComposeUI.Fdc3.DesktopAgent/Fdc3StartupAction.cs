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

using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Extensions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

/// <summary>
/// Default implementation how to handle FDC3 related startup actions for different module types in the FDC3 Desktop Agent infrastructure. 
/// It retrieves necessary information from the app directory and user channel set, and delegates the handling to specific module handlers based on the module type.
/// </summary>
internal sealed class Fdc3StartupAction : IStartupAction
{
    private readonly IAppDirectory _appDirectory;
    private readonly IUserChannelSetReader _userChannelSetReader;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly ILogger<Fdc3StartupAction> _logger;
    private static readonly Dictionary<string, IStartupModuleHandler> Handlers = new()
    {
        { ModuleType.Web, new WebStartupModuleHandler() },
        { ModuleType.Native, new NativeStartupModuleHandler() }
    };

    public Fdc3StartupAction(
        IAppDirectory appDirectory,
        IUserChannelSetReader userChannelSetReader,
        IOptions<Fdc3DesktopAgentOptions> options,
        ILogger<Fdc3StartupAction>? logger = null)
    {
        _appDirectory = appDirectory;
        _userChannelSetReader = userChannelSetReader;
        _options = options.Value;
        _logger = logger ?? NullLogger<Fdc3StartupAction>.Instance;
    }

    public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
    {
        try
        {
            var properties = await startupContext.GetFdc3Properties(_appDirectory, _userChannelSetReader, _logger).ConfigureAwait(false);

            var fdc3StartupProperties = new Fdc3StartupProperties { AppId = properties.AppId, InstanceId = properties.InstanceId, ChannelId = properties.ChannelId ?? _options.ChannelId, OpenedAppContextId = properties.OpenedAppContextId };
            var fdc3InstanceId = startupContext.GetOrAddProperty<Fdc3StartupProperties>(_ => fdc3StartupProperties).InstanceId;

            if (Handlers.TryGetValue(startupContext.ModuleInstance.Manifest.ModuleType, out var handler))
            {
                await handler.HandleAsync(startupContext, fdc3StartupProperties).ConfigureAwait(false);
            }
        }
        catch (AppNotFoundException exception)
        {
            _logger.LogError(exception, $"Fdc3 bundle js could be not added to the {startupContext.StartRequest.ModuleId}.");
        }

        await next.Invoke().ConfigureAwait(false);
    }
}
