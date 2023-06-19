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

using Google.Protobuf.Collections;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Extensions;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Client.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Client.Logging;
using ProcessExplorer.Abstractions.Infrastructure.Protos;
using ProcessInfoCollectorData = MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.ProcessInfoCollectorData;

namespace MorganStanley.ComposeUI.ProcessExplorer.Client.Infrastructure;

internal class GrpcCommunicator : ICommunicator
{
    private readonly ILogger<ICommunicator> _logger;
    private readonly ProcessExplorerMessageHandler.ProcessExplorerMessageHandlerClient _client;

    public GrpcCommunicator(
        IOptions<ClientServiceOptions> options,
        ILogger<ICommunicator>? logger = null)
    {
        var channel = GrpcChannel.ForAddress($"http://{options.Value.Host}:{options.Value.Port}/");
        var grpcClient = new ProcessExplorerMessageHandler.ProcessExplorerMessageHandlerClient(channel);

        _client = grpcClient;

        _logger = logger ?? NullLogger<GrpcCommunicator>.Instance;
    }

    public async ValueTask AddRuntimeInfo(KeyValuePair<RuntimeInformation, ProcessInfoCollectorData> runtimeInformation)
    {
        try
        {
            _logger.SendingClientRuntimeInformationDebug();

            var message = new Message()
            {
                Action = ActionType.AddRuntimeInfoAction,
                Description = "Add RuntimeInformation collected by LocalCollectors",
                AssemblyId = runtimeInformation.Key.Name,
                RuntimeInfo = runtimeInformation.Value.DeriveProtoRuntimeInfoType()
            };
            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddRuntimeInfoError(exception, exception);
        }
    }

    public async ValueTask AddConnectionCollection(KeyValuePair<RuntimeInformation, IEnumerable<IConnectionInfo>> connections)
    {
        try
        {
            if (!connections.Value.Any()) return;

            _logger.SendingClientConnectionCollectionDebug();

            var message = new Message()
            {
                Action = ActionType.AddConnectionListAction,
                Description = "Add connection collection collected by LocalCollector",
                AssemblyId = connections.Key.Name,
                Connections = { connections.Value.Select(connection => connection.DeriveProtoConnectionType()) }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.AddConnectionCollectionError(exception, exception);
        }
    }

    public async ValueTask UpdateConnectionInformation(KeyValuePair<RuntimeInformation, IConnectionInfo> connection)
    {
        try
        {
            _logger.SendingClientConnectionDebug(connection.Key.Name);

            var message = new Message()
            {
                Action = ActionType.UpdateConnectionAction,
                Description = "Update of a connection collected by LocalCollector",
                AssemblyId = connection.Key.Name,
                Connections = { new List<Connection>() { connection.Value.DeriveProtoConnectionType() } }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateConnectionInformationError(exception, exception);
        }
    }

    public async ValueTask UpdateEnvironmentVariableInformation(KeyValuePair<RuntimeInformation, IEnumerable<KeyValuePair<string, string>>> environmentVariables)
    {
        try
        {
            if (!environmentVariables.Value.Any()) return;

            _logger.SendingClientEnvironmentVariablesDebug(environmentVariables.Key.Name);

            var message = new Message()
            {
                Action = ActionType.UpdateEnvironmentVariablesAction,
                Description = "Update of environment variables collected by LocalCollector",
                AssemblyId = environmentVariables.Key.Name,
                EnvironmentVariables = { environmentVariables.Value.DeriveProtoDictionaryType() }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateEnvironmentVariableInformationError(exception, exception);
        }
    }

    public async ValueTask UpdateRegistrationInformation(KeyValuePair<RuntimeInformation, IEnumerable<RegistrationInfo>> registrations)
    {
        try
        {
            if (!registrations.Value.Any()) return;

            _logger.SendingClientRegistrationsDebug(registrations.Key.Name);

            var message = new Message()
            {
                Action = ActionType.UpdateRegistrationsAction,
                Description = "Update of registrations collected by LocalCollector",
                AssemblyId = registrations.Key.Name,
                Registrations = { registrations.Value.Select(registration => registration.DeriveProtoRegistrationType()) }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateRegistrationInformationError(exception, exception);
        }
    }

    public async ValueTask UpdateModuleInformation(KeyValuePair<RuntimeInformation, IEnumerable<ModuleInfo>> modules)
    {
        try
        {
            if (!modules.Value.Any()) return;

            _logger.SendingClientModulesDebug(modules.Key.Name);

            var message = new Message()
            {
                Action = ActionType.UpdateModulesAction,
                Description = "Update of modules collected by LocalCollector",
                AssemblyId = modules.Key.Name,
                Modules = { modules.Value.Select(module => module.DeriveProtoModuleType()) }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateModuleInformationError(exception, exception);
        }
    }

    public async ValueTask UpdateConnectionStatus(
        string assemblyId,
        string connectionId,
        ConnectionStatus connectionStatus)
    {
        try
        {
            _logger.SendingClientConnectionStatusDebug(connectionId);

            var message = new Message()
            {
                Action = ActionType.UpdateConnectionStatusAction,
                Description = "Update of a connection status change collected by LocalCollector",
                AssemblyId = assemblyId,
                ConnectionStatusChanges = { new MapField<string, string>() { { connectionId, connectionStatus.ToStringCached() } } }
            };

            await _client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.UpdateConnectionStatusError(connectionId, exception, exception);
        }
    }
}
