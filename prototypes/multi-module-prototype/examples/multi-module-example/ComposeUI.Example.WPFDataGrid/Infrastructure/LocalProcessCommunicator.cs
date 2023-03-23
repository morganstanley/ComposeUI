// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using LocalCollector;
using LocalCollector.Communicator;
using LocalCollector.Connections;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Messaging;
using ProcessExplorerMessageRouterTopics;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace WPFDataGrid.Infrastructure;

internal class LocalProcessCommunicator : ICommunicator
{
    private readonly IMessageRouter _messageRouter;
    private readonly ILogger<LocalProcessCommunicator> _logger;

    public LocalProcessCommunicator(IMessageRouter messageRouter,
        ILogger<LocalProcessCommunicator>? logger = null)
    {
        _messageRouter = messageRouter;
        _logger = logger ?? NullLogger<LocalProcessCommunicator>.Instance;
    }

    public async ValueTask AddConnectionCollection(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ConnectionInfo>>> connections)
    {
        try
        {
            var serializedConnections = JsonSerializer.Serialize(connections);
            await _messageRouter.PublishAsync(Topics.addingConnections, serializedConnections);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.addingConnections}... {exception}");
        }
    }

    public async ValueTask AddRuntimeInfo(IEnumerable<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>> listOfRuntimeInfo)
    {
        try
        {
            var serializedRuntimeInfo = JsonSerializer.Serialize(listOfRuntimeInfo);
            await _messageRouter.PublishAsync(Topics.updatingRuntime, serializedRuntimeInfo);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.updatingRuntime}... {exception}");
        }
    }

    public async ValueTask UpdateConnectionInformation(IEnumerable<KeyValuePair<AssemblyInformation, ConnectionInfo>> connections)
    {
        try
        {
            var serializedConnections = JsonSerializer.Serialize(connections);
            await _messageRouter.PublishAsync(Topics.updatingConnection, serializedConnections);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.updatingConnection}... {exception}");
        }
    }

    public async ValueTask UpdateEnvironmentVariableInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<KeyValuePair<string, string>>>> environmentVariables)
    {
        try
        {
            var serializedEnvs = JsonSerializer.Serialize(environmentVariables);
            await _messageRouter.PublishAsync(Topics.updatingEnvironmentVariables, serializedEnvs);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.updatingEnvironmentVariables}... {exception}");
        }
    }

    public async ValueTask UpdateModuleInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<ModuleInfo>>> modules)
    {
        try
        {
            var serializedModules = JsonSerializer.Serialize(modules);
            await _messageRouter.PublishAsync(Topics.updatingModules, serializedModules);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.updatingModules}... {exception}");
        }
    }

    public async ValueTask UpdateRegistrationInformation(IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<RegistrationInfo>>> registrations)
    {
        try
        {
            var serializedRegistrations = JsonSerializer.Serialize(registrations);
            await _messageRouter.PublishAsync(Topics.updatingRegistrations, serializedRegistrations);
        }
        catch (Exception exception)
        {
            _logger.LogInformation($"Some error(s) occurred while subscribing to topic: {Topics.updatingRegistrations}... {exception}");
        }
    }
}
