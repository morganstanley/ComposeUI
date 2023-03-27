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

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorer.Abstractions;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Factories;
using ProcessExplorerMessageRouterTopics;
using SuperRPC_POC.Infrastructure.Messages;

namespace SuperRPC_POC.Infrastructure;

public class ModuleLoaderInformationReceiver : IModuleLoaderInformationReceiver
{
    private readonly ILogger<ModuleLoaderInformationReceiver> _logger;
    private readonly IMessageRouter _messageRouter;
    private readonly IObserver<TopicMessage> _processInfoListMessage;
    private readonly IObserver<TopicMessage> _processInfoMessage;
    private readonly IObserver<TopicMessage> _processMonitorWatcherChecker;
    private readonly IObserver<TopicMessage> _runtimeObserver;
    public IProcessInfoAggregator? ProcessInfoAggregator { get; }
    private readonly ISubsystemController _subsystemController;
    private ISubsystemLauncherCommunicator _subsystemLauncherCommunicator;

    public ISubsystemController SubsystemController
    {
        get
        {
            return _subsystemController;
        }
    }

    public ModuleLoaderInformationReceiver(IProcessInfoAggregator aggregator,
        IMessageRouter messageRouter,
        ILoggerFactory loggerFactory,
        ProcessInfoListRouterMessage processInfoListMessage,
        ProcessInfoRouterMessage processInfoMessage,
        ProcessMonitorCheckerRouterMessage processMonitorCheckerRouterMessage,
        RuntimeInformationRouterMessage runtimeInformationRouterMessage,
        ILogger<ModuleLoaderInformationReceiver>? logger)
    {
        _logger = logger ?? NullLogger<ModuleLoaderInformationReceiver>.Instance;
        _messageRouter = messageRouter;
        ProcessInfoAggregator = aggregator;
        _processInfoListMessage = processInfoListMessage;
        _processInfoMessage = processInfoMessage;
        _processMonitorWatcherChecker = processMonitorCheckerRouterMessage;
        _runtimeObserver = runtimeInformationRouterMessage;

        //Subsystems Set-Up
        var subsystemsRouterMessage = new SubsystemRouterMessage(loggerFactory.CreateLogger<SubsystemRouterMessage>());

        _subsystemLauncherCommunicator
            = new SubsystemLauncherCommunicator(loggerFactory.CreateLogger<SubsystemLauncherCommunicator>(),
                messageRouter,
                subsystemsRouterMessage);

        _subsystemController
            = SubsystemFactory.CreateSubsystemController(_subsystemLauncherCommunicator,
                loggerFactory.CreateLogger<ISubsystemController>());

        subsystemsRouterMessage.SetSubsystemController(_subsystemController);

        //if you do not want to start the frontend of the PE comment out this code.
        //StartFrontend();
    }

    public async ValueTask SubscribeToProcessExplorerChangedElementTopicAsync()
    {
        _logger.LogInformation($"Subscribing to topic {Topics.changedProcessInfo} to receive information from moduleLoader");

        try
        {
            await _messageRouter.SubscribeAsync(Topics.changedProcessInfo, _processInfoMessage);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while subscribing to topic: process_explorer : {exception}");
        }
    }

    public async ValueTask SubscribeToEnableProcessMonitorTopicAsync()
    {
        _logger.LogInformation($"Subscribing to topic {Topics.enableProcessWatcher} to receive information from moduleLoader");

        try
        {
            await _messageRouter.SubscribeAsync(Topics.enableProcessWatcher, _processMonitorWatcherChecker);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while subscribing to topic: {Topics.enableProcessWatcher} : {exception}");
        }

    }

    public async ValueTask SubscribeToDisableProcessMonitorTopicAsync()
    {
        _logger.LogInformation($"Subscribing to topic {Topics.disableProcessWatcher} to receive information from moduleLoader");

        try
        {
            await _messageRouter.SubscribeAsync(Topics.disableProcessWatcher, _processMonitorWatcherChecker);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while subscribing to topic: {Topics.disableProcessWatcher} : {exception}");
        }
    }

    public async ValueTask SetSubsystemLauncherCommunicator(ISubsystemLauncherCommunicator subsystemCommunicator)
    {
        _subsystemLauncherCommunicator = subsystemCommunicator;
        await _subsystemLauncherCommunicator.InitializeCommunicationRoute();
    }

    public async ValueTask SubscribeToSubsystemsTopicAsync()
    {
        await _subsystemLauncherCommunicator.InitializeCommunicationRoute();
    }

    public ISubsystemLauncherCommunicator GetSubsystemLauncherCommunicator()
    {
        if (_subsystemLauncherCommunicator == null)
            throw new ArgumentNullException(nameof(_subsystemLauncherCommunicator));
        return _subsystemLauncherCommunicator;
    }

    public async ValueTask SubscribeToProcessExplorerTopicAsync()
    {
        _logger.LogInformation($"Subscribing to topic {Topics.watchingProcessChanges} to receive information from moduleLoader");

        try
        {
            await _messageRouter.SubscribeAsync(Topics.watchingProcessChanges, _processInfoListMessage);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some error(s) occurred while subscribing to topic: {Topics.watchingProcessChanges} : {exception}");
        }
    }

    public async ValueTask SubscribeToRuntimeInformationTopicAsync()
    {
        try
        {
            if (_messageRouter != null)
            {
                await _messageRouter.SubscribeAsync(Topics.addingConnections, _runtimeObserver);
                await _messageRouter.SubscribeAsync(Topics.updatingRuntime, _runtimeObserver);
                await _messageRouter.SubscribeAsync(Topics.updatingConnection, _runtimeObserver);
                await _messageRouter.SubscribeAsync(Topics.updatingEnvironmentVariables, _runtimeObserver);
                await _messageRouter.SubscribeAsync(Topics.updatingModules, _runtimeObserver);
                await _messageRouter.SubscribeAsync(Topics.updatingRegistrations, _runtimeObserver);
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing... {exception}");
        }
    }

    private void StartFrontend()
    {
        var ps1FilePath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\scripts\npm-frontend-start.ps1";
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = $"-File \"{ps1FilePath}\"",
            UseShellExecute = true,
        };

        Process.Start(processStartInfo);
    }
}
