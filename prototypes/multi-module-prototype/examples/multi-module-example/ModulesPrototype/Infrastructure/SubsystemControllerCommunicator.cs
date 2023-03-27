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

using ModulesPrototype.Infrastructure.Messages;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorerMessageRouterTopics;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorer.Abstractions.Subsystems;

namespace ModulesPrototype.Infrastructure;

internal class SubsystemControllerCommunicator : ISubsystemControllerCommunicator
{
    private readonly IMessageRouter _messageRouter;
    private readonly IObserver<TopicMessage> _subsystemHandlerObserver;
    private readonly ILogger<SubsystemControllerCommunicator> _logger;

    public SubsystemControllerCommunicator(
        ILogger<SubsystemControllerCommunicator>? logger,
        IMessageRouter messageRouter,
        SubsystemHandlerRouterMessage subsystemHandlerObserver)
    {
        _logger = logger ?? NullLogger<SubsystemControllerCommunicator>.Instance;
        _messageRouter = messageRouter;
        _subsystemHandlerObserver = subsystemHandlerObserver;
    }

    public async ValueTask InitializeCommunicationRoute()
    {
        //subscribing to topics, commands from UI
        try
        {
            await _messageRouter.SubscribeAsync(Topics.launchingSubsystemWithDelay, _subsystemHandlerObserver);
            await _messageRouter.SubscribeAsync(Topics.launchingSubsystems, _subsystemHandlerObserver);
            await _messageRouter.SubscribeAsync(Topics.restartingSubsystems, _subsystemHandlerObserver);
            await _messageRouter.SubscribeAsync(Topics.terminatingSubsystems, _subsystemHandlerObserver);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Some errors occurred while subscribing... {exception}");
        }
    }
}

