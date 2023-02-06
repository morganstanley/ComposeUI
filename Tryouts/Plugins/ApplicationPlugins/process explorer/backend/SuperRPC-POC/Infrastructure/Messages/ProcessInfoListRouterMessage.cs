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
using ProcessExplorer.Abstraction;
using ProcessExplorer.Abstraction.Processes;

namespace SuperRPC_POC.Infrastructure.Messages;

public class ProcessInfoListRouterMessage : IObserver<TopicMessage>
{
    private readonly ILogger<ProcessInfoListRouterMessage> _logger;
    private readonly IProcessInfoAggregator? _processInfoAggregator;

    public ProcessInfoListRouterMessage(ILogger<ProcessInfoListRouterMessage>? logger, IProcessInfoAggregator? processInfoAggregator)
    {
        _logger = logger ?? NullLogger<ProcessInfoListRouterMessage>.Instance;
        _processInfoAggregator = processInfoAggregator ?? null;
    }

    public void OnCompleted()
    {
        _logger.LogInformation("Received all of the information for the current message");
    }

    public void OnError(Exception exception)
    {
        _logger.LogError($"Some error(s) occurred while receiving the process information from module loader... : {exception}");
    }

    public void OnNext(TopicMessage value)
    {
        var payload = value.Payload;
        if (payload == null)
        {
            return;
        }

        var processInfo = JsonSerializer.Deserialize<ProcessInfoData[]>(payload.GetString());

        if (processInfo == null)
        {
            return;
        }

        _processInfoAggregator?.InitProcesses(processInfo.Select(p => p.PID).ToArray());
    }
}
