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
using LocalCollector.Connections;
using LocalCollector.EnvironmentVariables;
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
    public ProcessInfoCollectorData ProcessInformation { get; } = new();
    private ICommunicator? _communicator;
    private readonly ILogger<ProcessInfoCollector> _logger;
    private readonly RuntimeInformation _runtimeId = new();
    private readonly object _locker = new();

    public ProcessInfoCollector(
        ILogger<ProcessInfoCollector>? logger = null,
        ICommunicator? communicator = null,
        EnvironmentMonitorInfo? environmentVariables = null, 
        IConnectionMonitor? connections = null,
        RegistrationMonitorInfo? registrations = null, 
        ModuleMonitorInfo? modules = null,
        string? assemblyId = null, 
        int? processId = null)
    {
        _logger = logger ?? NullLogger<ProcessInfoCollector>.Instance;

        ProcessInformation.Id = processId ?? Process.GetCurrentProcess().Id;

        ProcessInformation.EnvironmentVariables = environmentVariables != null 
            ? environmentVariables.EnvironmentVariables 
            : EnvironmentMonitorInfo.FromEnvironment().EnvironmentVariables;

        ProcessInformation.Modules = modules != null 
            ? modules.CurrentModules 
            : ModuleMonitorInfo.FromAssembly().CurrentModules;

        if (assemblyId != null) _runtimeId.Name = assemblyId;

        if (connections != null)
        {
            ProcessInformation.Connections = connections.Connections.Connections;

            SetConnectionChangedEvent(connections);
        }

        if (registrations != null)
        {
            ProcessInformation.Registrations = registrations.Services;
        }

        if (communicator != null)
        {
            _communicator = communicator;
        }
    }

    private void SetConnectionChangedEvent(IConnectionMonitor connectionMonitor)
    {
        connectionMonitor._connectionStatusChanged += ConnectionStatusChangedHandler;
    }

    public void SetCommunicator(ICommunicator communicator)
    {
        _communicator = communicator;
        var runtimeInfo = new List<KeyValuePair<RuntimeInformation, ProcessInfoCollectorData>>()
        {
            new(_runtimeId, ProcessInformation)
        };

        _communicator.AddRuntimeInfo(runtimeInfo);
    }


    private void ConnectionStatusChangedHandler(object? sender, ConnectionInfo connection)
    {
        if (_communicator == null)
        {
            return;
        }

        try
        {
            lock (_locker)
            {
                var info = ProcessInformation.Connections.FirstOrDefault(p => p.Id == connection.Id);
                if (info != null)
                {
                    var index = ProcessInformation.Connections.IndexOf(info);
                    if (index >= 0 && ProcessInformation.Connections.Count <= index)
                    {
                        ProcessInformation.Connections[Convert.ToInt32(index)] = connection;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.ConnectionStatusChangedError(exception);
        }

        var connections = new List<KeyValuePair<RuntimeInformation, ConnectionInfo>>()
        {
            new(_runtimeId, connection)
        };

        _communicator.UpdateConnectionInformation(connections)
            .ConfigureAwait(true);
    }

    public async Task SendRuntimeInfo()
    {
        if (_communicator != null)
        {
            var runtimeInfo = new List<KeyValuePair<RuntimeInformation, ProcessInfoCollectorData>>()
            {
                new(_runtimeId, ProcessInformation)
            };

            await _communicator.AddRuntimeInfo(runtimeInfo);
        }
    }

    public async Task AddConnectionMonitor(ConnectionMonitorInfo connections)
    {
        if(_communicator != null)
            await AddOrUpdateElements(
                connections.Connections,
                ProcessInformation.Connections,
                (item) => (conn) => conn.Id == item.Id,
                _communicator.AddConnectionCollection);
    }

    public async Task AddConnectionMonitor(IConnectionMonitor connections)
    {
        await AddConnectionMonitor(connections.Connections);
    }

    public async Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables)
    {
        if (ProcessInformation.EnvironmentVariables.IsEmpty)
        {
            lock (_locker)
            {
                ProcessInformation.EnvironmentVariables = environmentVariables.EnvironmentVariables;
            }
        }
        else
        {
            foreach (var env in environmentVariables.EnvironmentVariables)
            {
                lock (_locker)
                {
                    ProcessInformation.EnvironmentVariables.AddOrUpdate(env.Key, env.Value, (_, _) => env.Value);
                }
            }

        }

        if (_communicator != null)
        {
            var info = new List<KeyValuePair<RuntimeInformation, IEnumerable<KeyValuePair<string, string>>>>()
            {
                new(_runtimeId, environmentVariables.EnvironmentVariables)
            };

            await _communicator.UpdateEnvironmentVariableInformation(info);
        }
    }

    private async Task AddOrUpdateElements<T>(
        SynchronizedCollection<T> source,
        SynchronizedCollection<T> target,
        Func<T, Func<T, bool>> predicate,
        Func<IEnumerable<KeyValuePair<RuntimeInformation, IEnumerable<T>>>, ValueTask> handler)
    {
        if (!target.Any())
        {
            lock (_locker)
            {
                target = source;
            }
        }
        else
        {
            foreach (var item in source)
            {
                lock (_locker)
                {
                    var element = target.FirstOrDefault(predicate(item));
                    if (element == null)
                    {
                        continue;
                    }

                    var index = target.IndexOf(element);
                    if(index != -1)
                    {
                        target[index] = item;
                    }
                    else
                    {
                        target.Add(item);
                    }
                }
            }
        }

        if(_communicator != null)
        {
            var info = new List<KeyValuePair<RuntimeInformation, IEnumerable<T>>>()
            {
                new(_runtimeId, source)
            };

            await handler(info);
        }
    }

    public async Task AddRegistrations(RegistrationMonitorInfo registrations)
    {
        if (_communicator != null)
            await AddOrUpdateElements(
                registrations.Services,
                ProcessInformation.Registrations,
                (item) => (reg) => reg.LifeTime == item.LifeTime && reg.ImplementationType == item.ImplementationType && reg.LifeTime == item.LifeTime,
                _communicator.UpdateRegistrationInformation);
    }

    public async Task AddModules(ModuleMonitorInfo modules)
    {
        if (_communicator != null)
            await AddOrUpdateElements(
                modules.CurrentModules,
                ProcessInformation.Modules,
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
        _runtimeId.Name = assemblyId;
    }

    public void SetClientPid(int clientPid)
    {
        ProcessInformation.Id = clientPid;
    }
}
