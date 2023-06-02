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

using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using MorganStanley.ComposeUI.ProcessExplorer.Client.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Client.Logging;

namespace MorganStanley.ComposeUI.ProcessExplorer.Client;

internal class ProcessInfoHandler : IProcessInfoHandler
{
    private readonly ProcessInfoCollectorData _processInformation = new();
    private readonly ICommunicator _communicator;
    private readonly ILogger<IProcessInfoHandler> _logger;
    private readonly RuntimeInformation _runtimeId = new();
    private readonly object _processInformationLocker = new();
    private readonly object _runtimeInformationLocker = new();

    public ProcessInfoHandler(
        ICommunicator communicator,
        ILogger<IProcessInfoHandler>? logger = null,
        IOptions<ClientServiceOptions>? options = null)
    {
        _logger = logger ?? NullLogger<IProcessInfoHandler>.Instance;
        _communicator = communicator;
        _processInformation.Id = options?.Value.ProcessId ?? Environment.ProcessId;
        _processInformation.EnvironmentVariables = options?.Value.EnvironmentVariables ?? InformationHandlerHelper.GetEnvironmentVariablesFromAssembly(_logger);
        _processInformation.Modules = options?.Value.Modules ?? InformationHandlerHelper.GetModulesFromAssembly();
        _runtimeId.Name = options?.Value.AssemblyId ?? string.Empty;

        if (options?.Value.LoadedServices != null) _processInformation.Registrations = InformationHandlerHelper.GetRegistrations(options.Value.LoadedServices);

        if (options?.Value.Connections == null) return;

        _processInformation.Connections = options.Value.Connections;

        AddConnectionSubscription(_runtimeId.Name, _processInformation.Connections);
    }

    private void AddConnectionSubscription(string runtimeId, IEnumerable<IConnectionInfo> connections)
    {
        foreach (var connection in connections)
        {
            connection.ConnectionStatusEvents
                .Select(connectionKvp =>
                    Observable.FromAsync(async () =>
                    {
                        await _communicator.UpdateConnectionStatus(
                            runtimeId,
                            connectionKvp.Key,
                            connectionKvp.Value);

                        _logger.ConnectionUpdatedDebug(connectionKvp.Key, connectionKvp.Value.ToStringCached());

                    }))
                .Concat()
                .Subscribe();
        }
    }

    public ValueTask SendRuntimeInfo()
    {
        lock (_processInformationLocker)
        {
            lock (_runtimeInformationLocker)
            {
                _logger.SendingLocalCollectorRuntimeInformationWithIdDebug(_runtimeId.Name);
                return _communicator.AddRuntimeInfo(new(_runtimeId, _processInformation));
            }
        }
    }

    public ValueTask AddConnections(IEnumerable<IConnectionInfo> connections)
    {
        lock (_processInformationLocker)
        {
            lock (_runtimeInformationLocker)
            {
                _processInformation.AddOrUpdateConnections(connections);
                AddConnectionSubscription(_runtimeId.Name, connections);
                _logger.SendingLocalCollectorConnectionCollectionWithIdDebug(_runtimeId.Name);
                return _communicator.AddConnectionCollection(new KeyValuePair<RuntimeInformation, IEnumerable<IConnectionInfo>>(_runtimeId, connections));
            }
        }
    }

    public ValueTask AddEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> environmentVariables)
    {
        lock (_processInformationLocker)
        {
            lock (_runtimeInformationLocker)
            {
                _processInformation.UpdateOrAddEnvironmentVariables(environmentVariables);
                _logger.SendingLocalCollectorEnvironmentVariablesDebug(_runtimeId.Name);
                return _communicator.UpdateEnvironmentVariableInformation(
                    new KeyValuePair<RuntimeInformation, IEnumerable<KeyValuePair<string, string>>>(
                        _runtimeId,
                        environmentVariables));
            }
        }
    }

    public ValueTask AddRegistrations(IEnumerable<RegistrationInfo> registrations)
    {
        lock (_processInformationLocker)
        {
            lock (_runtimeInformationLocker)
            {
                _processInformation.UpdateOrAddRegistrations(registrations);
                _logger.SendingLocalCollectorRegistrationsDebug(_runtimeId.Name);
                return _communicator.UpdateRegistrationInformation(
                    new KeyValuePair<RuntimeInformation, IEnumerable<RegistrationInfo>>(_runtimeId, registrations));
            }
        }
    }

    public ValueTask AddModules(IEnumerable<ModuleInfo> modules)
    {
        lock (_processInformationLocker)
        {
            lock (_runtimeInformationLocker)
            {
                _processInformation.UpdateOrAddModules(modules);
                _logger.SendingLocalCollectorModulesDebug(_runtimeId.Name);
                return _communicator.UpdateModuleInformation(new KeyValuePair<RuntimeInformation, IEnumerable<ModuleInfo>>(_runtimeId, modules));
            }
        }
    }

    public async ValueTask AddRuntimeInformation(
        IEnumerable<IConnectionInfo> connections,
        IEnumerable<KeyValuePair<string, string>> environmentVariables,
        IEnumerable<RegistrationInfo> registrations,
        IEnumerable<ModuleInfo> modules)
    {
        await AddConnections(connections);
        await AddEnvironmentVariables(environmentVariables);
        await AddRegistrations(registrations);
        await AddModules(modules);
        await SendRuntimeInfo();
    }

    public void SetAssemblyId(string assemblyId)
    {
        lock (_runtimeInformationLocker)
        {
            _runtimeId.Name = assemblyId;
        }
    }

    public void SetClientPid(int clientPid)
    {
        lock (_processInformationLocker)
        {
            _processInformation.Id = clientPid;
        }
    }

    public ProcessInfoCollectorData GetProcessInfoCollectorData()
    {
        lock (_processInformationLocker)
        {
            return _processInformation;
        }
    }
}
