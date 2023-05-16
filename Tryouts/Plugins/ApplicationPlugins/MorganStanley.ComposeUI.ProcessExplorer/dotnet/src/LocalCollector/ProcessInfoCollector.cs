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

using System.Diagnostics;
using LocalCollector.EnvironmentVariables;
using LocalCollector.Extensions;
using LocalCollector.Logging;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstractions.Entities;
using ProcessExplorer.Abstractions.Entities.Connections;
using ProcessExplorer.Abstractions.Infrastructure;

namespace LocalCollector;

public class ProcessInfoCollector : IProcessInfoCollector
{
    private readonly ProcessInfoCollectorData _processInformation = new();
    private readonly ICommunicator _communicator;
    private readonly ILogger<ProcessInfoCollector> _logger;
    private readonly RuntimeInformation _runtimeId = new();
    private readonly object _locker = new();

    public ProcessInfoCollector(
        ICommunicator communicator,
        ILogger<ProcessInfoCollector>? logger = null,
        EnvironmentMonitorInfo? environmentVariables = null,
        IConnectionMonitor? connections = null,
        RegistrationMonitorInfo? registrations = null,
        ModuleMonitorInfo? modules = null,
        string? assemblyId = null,
        int? processId = null)
    {
        _logger = logger ?? NullLogger<ProcessInfoCollector>.Instance;

        _processInformation.Id = processId ?? Process.GetCurrentProcess().Id;

        _processInformation.EnvironmentVariables = environmentVariables != null
            ? environmentVariables.EnvironmentVariables
            : EnvironmentMonitorInfo.FromEnvironment().EnvironmentVariables;

        _processInformation.Modules = modules != null
            ? modules.CurrentModules
            : ModuleMonitorInfo.FromAssembly().CurrentModules;

        if (assemblyId != null) _runtimeId.Name = assemblyId;

        if (connections != null)
        {
            _processInformation.Connections = connections.Connections.Connections;

            SetConnectionChangedEvent(connections);
        }

        if (registrations != null)
        {
            _processInformation.Registrations = registrations.Services;
        }

        _communicator = communicator;
    }

    private void SetConnectionChangedEvent(IConnectionMonitor connectionMonitor)
    {
        connectionMonitor._connectionStatusChanged += ConnectionStatusChangedHandler;
    }

    private async void ConnectionStatusChangedHandler(object? sender, ConnectionInfo connection)
    {
        IEnumerable<KeyValuePair<RuntimeInformation, ConnectionInfo>> connections;

        try
        {
            lock (_locker)
            {
                var info = _processInformation.Connections.FirstOrDefault(p => p.Id == connection.Id);
                if (info != null)
                {
                    var index = _processInformation.Connections.IndexOf(info);
                    if (index >= 0 && _processInformation.Connections.Count <= index)
                    {
                        _processInformation.Connections[Convert.ToInt32(index)] = connection;
                    }
                }

                connections = new List<KeyValuePair<RuntimeInformation, ConnectionInfo>>()
                {
                    new(_runtimeId, connection)
                };
            }
        }
        catch (Exception exception)
        {
            _logger.ConnectionStatusChangedError(exception);
            return;
        }

        if (connections.Any())
            await _communicator.UpdateConnectionInformation(connections);
    }

    public async Task SendRuntimeInfo()
    {
        IEnumerable<KeyValuePair<RuntimeInformation, ProcessInfoCollectorData>> runtimeInfo;

        lock(_locker)
        {
            runtimeInfo = new List<KeyValuePair<RuntimeInformation, ProcessInfoCollectorData>>()
            {
                new(_runtimeId, _processInformation)
            };
        }

        await _communicator.AddRuntimeInfo(runtimeInfo);
    }

    public async Task AddConnectionMonitor(ConnectionMonitorInfo connections)
    {
        await AddOrUpdateElements(
                connections.Connections,
                _processInformation.Connections,
                (item) => (conn) => conn.Id == item.Id,
                _communicator.AddConnectionCollection);
    }

    public async Task AddConnectionMonitor(IConnectionMonitor connections)
    {
        await AddConnectionMonitor(connections.Connections);
    }

    public async Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables)
    {
        IEnumerable<KeyValuePair<RuntimeInformation, IEnumerable<KeyValuePair<string, string>>>> info;

        lock (_locker)
        {
            foreach (var env in environmentVariables.EnvironmentVariables)
            {
                _processInformation.EnvironmentVariables.AddOrUpdate(env.Key, env.Value, (_, _) => env.Value);
            }

            info = new List<KeyValuePair<RuntimeInformation, IEnumerable<KeyValuePair<string, string>>>>()
            {
                new(_runtimeId, environmentVariables.EnvironmentVariables)
            };
        }

        if (info.Any())
        {
            await _communicator.UpdateEnvironmentVariableInformation(info);
        }
    }

    private async Task AddOrUpdateElements<T>(
        IEnumerable<T> source, 
        IEnumerable<T> target,
        Func<T, Func<T, bool>> predicate,
        Func<IEnumerable<KeyValuePair<RuntimeInformation, IEnumerable<T>>>, ValueTask> handler)
    {
        IEnumerable<KeyValuePair<RuntimeInformation, IEnumerable<T>>> info;

        lock (_locker)
        {
            if (!target.Any()) target = source;
            else
            {
                foreach (var item in source)
                {
                    var element = target.FirstOrDefault(predicate(item));

                    if (element == null) continue;

                    var index = target.IndexOf(element);

                    if (index == -1)
                    {
                        target = target.Append(item);
                    }
                    else
                    {
                        target = target.Replace(index, element);
                    }
                }
            }

            info = new List<KeyValuePair<RuntimeInformation, IEnumerable<T>>>() { new(_runtimeId, source) };
        }

        await handler(info);
    }

    public async Task AddRegistrations(RegistrationMonitorInfo registrations)
    {
        await AddOrUpdateElements(
            registrations.Services,
            _processInformation.Registrations,
            (item) => (reg) => reg.LifeTime == item.LifeTime && reg.ImplementationType == item.ImplementationType && reg.LifeTime == item.LifeTime,
            _communicator.UpdateRegistrationInformation);
    }

    public async Task AddModules(ModuleMonitorInfo modules)
    {
        await AddOrUpdateElements(
            modules.CurrentModules,
            _processInformation.Modules,
            (item) => (mod) => mod.Name == item.Name && mod.PublicKeyToken == item.PublicKeyToken && mod.Version == item.Version,
            _communicator.UpdateModuleInformation);
    }

    public async Task AddRuntimeInformation(IConnectionMonitor connections,
        EnvironmentMonitorInfo environmentVariables,
        RegistrationMonitorInfo registrations, ModuleMonitorInfo modules)
    {
        await AddConnectionMonitor(connections);
        await AddEnvironmentVariables(environmentVariables);
        await AddRegistrations(registrations);
        await AddModules(modules);
    }

    public void SetAssemblyId(string assemblyId)
    {
        lock (_locker)
        {
            _runtimeId.Name = assemblyId;
        }
    }

    public void SetClientPid(int clientPid)
    {
        lock (_locker)
        {
            _processInformation.Id = clientPid;
        }
    }
}
