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

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorerMessageRouterTopics;
using SuperRPC_POC.Infrastructure.Messages;

namespace SuperRPC_POC.Infrastructure;

public class SubsystemLauncherCommunicator : ISubsystemLauncherCommunicator
{
    private readonly ILogger<SubsystemLauncherCommunicator> _logger;
    private readonly IMessageRouter? _messageRouter;
    private readonly IObserver<TopicMessage> _subsystemsObserver;

    public SubsystemLauncherCommunicator(ILogger<SubsystemLauncherCommunicator>? logger,
        IMessageRouter? messageRouter,
        SubsystemRouterMessage subsystemsObserver)
    {
        _logger = logger ?? NullLogger<SubsystemLauncherCommunicator>.Instance;
        _messageRouter = messageRouter;
        _subsystemsObserver = subsystemsObserver;
    }

    public async Task InitializeCommunicationRoute()
    {
        _logger.LogInformation($"Getting the collection of subsystems to Process Explorer to show on the UI.");
        try
        {
            if (_messageRouter != null)
            {
                await _messageRouter.SubscribeAsync(Topics.initilaizingSubsystems, _subsystemsObserver);
                await _messageRouter.SubscribeAsync(Topics.addingSubsystem, _subsystemsObserver);
                await _messageRouter.SubscribeAsync(Topics.modifyingSubsystem, _subsystemsObserver);
                await _messageRouter.SubscribeAsync(Topics.removingSubsystem, _subsystemsObserver);
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing... {exception}");
        }
    }

    public async Task SendLaunchSubsystemAfterTimeRequest(Guid subsystemId, int periodOfTime)
    {
        _logger.LogInformation($"Publishing launch after time request to SubsystemControllerCommunicator to handle subsystems...");

        try
        {
            if (_messageRouter != null)
            {
                var serializableSubsystem = new KeyValuePair<Guid, int>(subsystemId, periodOfTime);
                await _messageRouter.PublishAsync(Topics.launchingSubsystemWithDelay, JsonSerializer.Serialize(serializableSubsystem));
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.launchingSubsystemWithDelay}... {exception}");
        }
    }

    public async Task SendLaunchSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        _logger.LogInformation($"Publishing launch request to SubsystemControllerCommunicator to handle subsystems...");

        try
        {
            if (_messageRouter != null)
            {
                await _messageRouter.PublishAsync(Topics.launchingSubsystems, JsonSerializer.Serialize(subsystems));
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.launchingSubsystems}... {exception}");
        }
    }

    public async Task SendRestartSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        _logger.LogInformation($"Publishing restart request to SubsystemControllerCommunicator to handle subsystems...");

        try
        {
            if (_messageRouter != null)
            {
                await _messageRouter.PublishAsync(Topics.restartingSubsystems, JsonSerializer.Serialize(subsystems));
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.restartingSubsystems}... {exception}");
        }
    }

    public async Task SendShutdownSubsystemsRequest(IEnumerable<Guid> subsystems)
    {
        _logger.LogInformation($"Publishing stop request to SubsystemControllerCommunicator to handle subsystems...");

        try
        {
            if (_messageRouter != null)
            {
                await _messageRouter.PublishAsync(Topics.terminatingSubsystems, JsonSerializer.Serialize(subsystems));
            }
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while publishing to topic: {Topics.terminatingSubsystems}... {exception}");
        }
    }
}

