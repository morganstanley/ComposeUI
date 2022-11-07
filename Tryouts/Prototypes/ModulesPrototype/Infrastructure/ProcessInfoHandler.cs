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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Processes;
using ModuleProcessMonitor.Subsystems;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using ProcessExplorerMessageRouterTopics;

namespace ModulesPrototype.Infrastructure;

public class ProcessInfoHandler : IProcessInfoHandler
{
    private readonly ILogger<ProcessInfoHandler> _logger;
    private readonly IMessageRouter _messageRouter;
    private IModuleLoader? _moduleLoader;
    private ISubsystemLauncher? _subsystemLauncher;
    private ISubsystemControllerCommunicator? _subsystemControllerCommunicator;

    public ProcessInfoHandler(ILogger<ProcessInfoHandler>? logger,
        IMessageRouter messageRouter)
    {
        _logger = logger ?? NullLogger<ProcessInfoHandler>.Instance;
        _messageRouter = messageRouter;
    }

    public async ValueTask SendInitProcessInfoAsync(IEnumerable<ProcessInformation> processInfo)
    {
        _logger.LogInformation("Sending the collection of processInformation....");

        try
        {
            Thread.Sleep(1000);

            if (processInfo.Any())
            {
                var listOfProcessInfoData = processInfo
                  .Select(process => process?.ProcessInfo);

                var serializedListOfProcessInfo = JsonSerializer.Serialize(listOfProcessInfoData);

                await _messageRouter.PublishAsync(Topics.watchingProcessChanges, serializedListOfProcessInfo);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while publishing the list of processInfo through topic : {Topics.watchingProcessChanges}.. : {exception}");
        }
    }

    public void SendAddProcessInfo(ProcessInfoData processInfo)
    {
        _logger.LogInformation("Sending a process....");

        try
        {
            var serializedProcessInfo = JsonSerializer.Serialize(processInfo);

            //fire - and - forget
            _messageRouter.PublishAsync(Topics.changedProcessInfo, serializedProcessInfo);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while publishing processInfo through topic : {Topics.changedProcessInfo}.. : {exception}");
        }
    }

    public async ValueTask EnableProcessMonitorAsync()
    {
        _logger.LogInformation("Sending key to watch processes....");

        try
        {
            await _messageRouter.PublishAsync(Topics.enableProcessWatcher, string.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while publishing the command to set processMonitor's watcher through topic : {Topics.enableProcessWatcher}.. : {exception}");
        }
    }

    public async ValueTask DisableProcessMonitorAsync()
    {
        _logger.LogInformation("Sending key to unwatch processes....");

        try
        {
            await _messageRouter.PublishAsync(Topics.disableProcessWatcher, string.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while publishing the command to unset processMonitor's watcher through topic : {Topics.disableProcessWatcher}.. : {exception}");
        }
    }

    public async ValueTask SendModifiedSubsystemStateAsync(Guid instanceId, string state)
    {
        try
        {
            if (_subsystemLauncher == null) return;

            await _subsystemLauncher.ModifySubsystemState(instanceId, state);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some errors occurred while sending modify subsystem state request. {exception}");
        }
    }

    public async ValueTask InitializeSubsystemControllerRouteAsync()
    {
        try
        {
            if (_subsystemControllerCommunicator == null) return;

            await _subsystemControllerCommunicator.InitializeCommunicationRoute();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some errors occurred while Initializing user defined communication route. {exception}");
        }
    }

    public async ValueTask SendRegisteredSubsystemsAsync(string subsystems)
    {
        try
        {
            Thread.Sleep(5000);
            if (_subsystemLauncher == null) return;

            _subsystemLauncher.SetSubsystems(subsystems);
            await _subsystemLauncher.InitSubsystems();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some errors occurred while sending subsystems to the Process Explorer backend. {exception}");
        }
    }

    public void SetSubsystemHandler(IModuleLoader moduleLoader,
        ILoggerFactory? loggerFactory = null)
    {
        if (loggerFactory == null) return;
        _moduleLoader = moduleLoader;
        var subsystemServiceProvider = new ServiceCollection()
            .AddSubsystemHandler(builder =>
            {
                builder.Configure(loggerFactory,
                    _messageRouter,
                    _moduleLoader);
            })
            .BuildServiceProvider();

        _subsystemLauncher = subsystemServiceProvider
            .GetRequiredService<ISubsystemLauncher>();

        _subsystemControllerCommunicator = subsystemServiceProvider
            .GetRequiredService<ISubsystemControllerCommunicator>();
    }
}
