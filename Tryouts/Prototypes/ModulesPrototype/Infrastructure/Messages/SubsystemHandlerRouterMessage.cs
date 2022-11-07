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
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModuleProcessMonitor.Subsystems;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorerMessageRouterTopics;

namespace ModulesPrototype.Infrastructure.Messages;

internal class SubsystemHandlerRouterMessage : IObserver<RouterMessage>
{
    private readonly ILogger<SubsystemHandlerRouterMessage> _logger;
    private readonly ISubsystemLauncher _subsystemLauncher;

    public SubsystemHandlerRouterMessage(ILogger<SubsystemHandlerRouterMessage>? logger,
        ISubsystemLauncher subsystemLauncher)
    {
        _logger = logger ?? NullLogger<SubsystemHandlerRouterMessage>.Instance;
        _subsystemLauncher = subsystemLauncher;
    }

    public void OnCompleted()
    {
        _logger.LogInformation("Received all of the information for the current message");
    }

    public void OnError(Exception exception)
    {
        _logger.LogError($"Some error(s) occurred while receiving the process information from module loader... : {exception}");
    }

    public void OnNext(RouterMessage value)
    {
        var payload = value.Payload;
        if (payload is null)
        {
            return;
        }
        
        var topic = value.Topic;
        try
        {
            switch (topic)
            {
                case Topics.launchingSubsystemWithDelay:
                    var subsystem = JsonSerializer.Deserialize<KeyValuePair<Guid, int>>(payload.GetString());

                    _subsystemLauncher.LaunchSubsystemAfterTime(subsystem.Key, subsystem.Value);

                    break;

                case Topics.launchingSubsystems:
                    var subsystemsToStart = JsonSerializer.Deserialize<List<Guid>>(payload.GetString());

                    if (subsystemsToStart != null) _subsystemLauncher.LaunchSubsystems(subsystemsToStart);

                    break;

                case Topics.restartingSubsystems:
                    var subsystemsToRestart = JsonSerializer.Deserialize<List<Guid>>(payload.GetString());

                    if (subsystemsToRestart != null) _subsystemLauncher.RestartSubsystems(subsystemsToRestart);

                    break;

                case Topics.terminatingSubsystems:
                    var subsystemsToShutDown = JsonSerializer.Deserialize<List<Guid>>(payload.GetString());

                    if (subsystemsToShutDown != null) _subsystemLauncher.ShutdownSubsystems(subsystemsToShutDown);

                    break;
            }
        }
        catch(Exception exception)
        {
            _logger.LogError($"Errors occurred while sending launch/restart/shutdown command to the SubsystemLauncher. {exception}.");
        }
    }
}

