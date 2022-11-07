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
using LocalCollector;
using LocalCollector.Communicator;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorer;
using ProcessExplorerMessageRouterTopics;
using ConnectionInfo = LocalCollector.Connections.ConnectionInfo;

namespace SuperRPC_POC.Infrastructure.Messages;

public class RuntimeInformationRouterMessage : IObserver<RouterMessage>
{
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly ILogger<RuntimeInformationRouterMessage> _logger;

    public RuntimeInformationRouterMessage(IProcessInfoAggregator processInfoAggregator,
        ILogger<RuntimeInformationRouterMessage>? logger = null)
    {
        _processInfoAggregator = processInfoAggregator;
        _logger = logger ?? NullLogger<RuntimeInformationRouterMessage>.Instance;
    }

    public void OnCompleted()
    {
        _logger.LogInformation("Received all of the information for the current message");
    }

    public void OnError(Exception exception)
    {
        _logger.LogError($"Some error(s) occurred while receiving the process monitor checker from module loader... : {exception}");
    }

    public async void OnNext(RouterMessage value)
    {
        var topic = value.Topic;
        var payload = value.Payload;

        if (payload is null)
        {
            return;
        }

        switch (topic)
        {
            case Topics.addingConnections:

                var connections = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ConnectionInfo>>>>(payload.GetString());

                if (connections == null)
                {
                    return;
                }

                foreach (var connection in connections)
                {
                    if (connection.Key == null) continue;
                    await _processInfoAggregator.AddConnectionCollection(connection.Key.Name, connection.Value);
                }

                break;

            case Topics.updatingRuntime:
                var runtimeInfo = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>>>(payload.GetString());

                if (runtimeInfo == null)
                {
                    return;
                }

                foreach (var runtime in runtimeInfo)
                {
                    if (runtime.Key == null || runtime.Value == null) continue;
                    await _processInfoAggregator.AddInformation(runtime.Key.Name, runtime.Value);
                }

                break;

            case Topics.updatingConnection:
                var connectionUpdates = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, ConnectionInfo>>>(payload.GetString());

                if (connectionUpdates == null)
                {
                    return;
                }

                foreach(var connectionUpdate in connectionUpdates)
                {
                    if(connectionUpdate.Key == null || connectionUpdate.Value == null) continue;
                    await _processInfoAggregator.UpdateConnectionInfo(connectionUpdate.Key.Name, connectionUpdate.Value);
                }

                break;

            case Topics.updatingEnvironmentVariables:
                var envs = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<KeyValuePair<string, string>>>>>(payload.GetString());

                if (envs == null)
                {
                    return;
                }

                foreach(var env in envs)
                {
                    if(env.Key == null || env.Value == null) continue;
                    await _processInfoAggregator.UpdateEnvironmentVariablesInfo(env.Key.Name, env.Value);
                }

                break;

            case Topics.updatingModules:
                var modules = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ModuleInfo>>>>(payload.GetString());

                if (modules == null)
                {
                    return;
                }

                foreach( var module in modules)
                {
                    if(module.Key == null || module.Value == null) continue;
                    await _processInfoAggregator.UpdateModuleInfo(module.Key.Name, module.Value);
                }
                break;

            case Topics.updatingRegistrations:
                var registrations = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<RegistrationInfo>>>>(payload.GetString());

                if (registrations == null)
                {
                    return;
                }

                foreach(var registration in registrations)
                {
                    if(registration.Key == null || registration.Value == null) continue;
                    await _processInfoAggregator.UpdateRegistrationInfo(registration.Key.Name, registration.Value);
                }
                break;
        }

    }
}
