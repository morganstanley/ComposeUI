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
using ProcessExplorer.Abstraction.Subsystems;
using ProcessExplorerMessageRouterTopics;

namespace SuperRPC_POC.Infrastructure.Messages;

public class SubsystemRouterMessage : IObserver<TopicMessage>
{
    private readonly ILogger<SubsystemRouterMessage> _logger;
    private ISubsystemController? _subsystemController;

    public SubsystemRouterMessage(ILogger<SubsystemRouterMessage>? logger)
    {
        _logger = logger ?? NullLogger<SubsystemRouterMessage>.Instance;
    }

    public void SetSubsystemController(ISubsystemController subsystemController)
    {
        _subsystemController = subsystemController;
    }

    public void OnCompleted()
    {
        _logger.LogInformation("Received all of the information for the current message");
    }

    public void OnError(Exception exception)
    {
        _logger.LogError(
            $"Some error(s) occurred while receiving the process information from module loader... : {exception}");
    }

    public void OnNext(TopicMessage value)
    {
        var topic = value.Topic;
        var payload = value.Payload;

        if (payload is null)
        {
            return;
        }

        switch (topic)
        {
            case Topics.initilaizingSubsystems:

                var initSubsystems = JsonSerializer.Deserialize<Dictionary<Guid, SubsystemInfo>>(payload.GetString());

                if (initSubsystems == null)
                {
                    return;
                }

                _subsystemController.InitializeSubsystems(initSubsystems);

                break;

            case Topics.addingSubsystem:
                var addSubsystem = JsonSerializer.Deserialize<KeyValuePair<Guid, SubsystemInfo>>(payload.GetString());

                if (addSubsystem.Key == null)
                {
                    return;
                }

                _subsystemController.AddSubsystem(addSubsystem.Key, addSubsystem.Value);

                break;

            case Topics.modifyingSubsystem:
                var modifySubsystem = JsonSerializer.Deserialize<KeyValuePair<Guid, string>>(payload.GetString());

                if (modifySubsystem.Key == null)
                {
                    return;
                }

                _subsystemController.ModifySubsystemState(modifySubsystem.Key, modifySubsystem.Value);

                break;

            case Topics.removingSubsystem:
                var subsystemId = JsonSerializer.Deserialize<Guid>(payload.GetString());

                if (subsystemId == null)
                {
                    return;
                }

                _subsystemController.RemoveSubsystem(subsystemId);

                break;
        }
    }
}
