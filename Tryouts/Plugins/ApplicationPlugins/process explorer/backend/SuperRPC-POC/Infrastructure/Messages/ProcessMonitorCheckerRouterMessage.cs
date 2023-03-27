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

using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorer.Abstractions;
using ProcessExplorerMessageRouterTopics;

namespace SuperRPC_POC.Infrastructure.Messages;

public class ProcessMonitorCheckerRouterMessage : IObserver<TopicMessage>
{
    private readonly ILogger<ProcessMonitorCheckerRouterMessage> _logger;
    private readonly IProcessInfoAggregator? _processInfoAggregator;

    public ProcessMonitorCheckerRouterMessage(ILogger<ProcessMonitorCheckerRouterMessage>? logger,
        IProcessInfoAggregator? processInfoAggregator)
    {
        _processInfoAggregator = processInfoAggregator ?? null;
        _logger = logger ?? NullLogger<ProcessMonitorCheckerRouterMessage>.Instance;
    }

    public void OnCompleted()
    {
        _logger.LogInformation("Received all of the information for the current message");
    }

    public void OnError(Exception exception)
    {
        _logger.LogError($"Some error(s) occurred while receiving the process monitor checker from module loader... : {exception}");
    }

    public void OnNext(TopicMessage value)
    {
        var topic = value.Topic;

        switch (topic)
        {
            case Topics.enableProcessWatcher:
                _processInfoAggregator?.EnableWatchingSavedProcesses();
                break;
            case Topics.disableProcessWatcher:
                _processInfoAggregator?.DisableWatchingProcesses();
                break;
        }
    }
}

