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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal sealed class Fdc3StartupAction : IStartupAction
{
    private readonly IAppDirectory _appDirectory;
    private readonly IUserChannelSetReader _userChannelSetReader;
    private readonly Fdc3DesktopAgentOptions _options;
    private readonly ILogger<Fdc3StartupAction> _logger;
    private static readonly Dictionary<string, StartupModuleHandler> Handlers = new()
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
            var appId = (await _appDirectory.GetApp(startupContext.StartRequest.ModuleId)).AppId;
            var userChannelSet = await _userChannelSetReader.GetUserChannelSet();

            var fdc3InstanceId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3InstanceId).Value
                                 ?? Guid.NewGuid().ToString();

            var channelId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(parameter => parameter.Key == Fdc3StartupParameters.Fdc3ChannelId).Value ?? _options.ChannelId
                    ?? userChannelSet.FirstOrDefault().Key;

            var openedAppContextId = startupContext
                .StartRequest
                .Parameters
                .FirstOrDefault(x => x.Key == Fdc3StartupParameters.OpenedAppContextId).Value;

            var fdc3StartupProperties = new Fdc3StartupProperties { InstanceId = fdc3InstanceId, ChannelId = channelId, OpenedAppContextId = openedAppContextId };
            fdc3InstanceId = startupContext.GetOrAddProperty<Fdc3StartupProperties>(_ => fdc3StartupProperties).InstanceId;

            if (Handlers.TryGetValue(startupContext.ModuleInstance.Manifest.ModuleType, out var handler))
            {
                await handler.HandleAsync(startupContext, appId, fdc3InstanceId, channelId, openedAppContextId);
            }
        }
        catch (AppNotFoundException exception)
        {
            _logger.LogError(exception, $"Fdc3 bundle js could be not added to the {startupContext.StartRequest.ModuleId}.");
        }

        await next.Invoke();
    }
}
