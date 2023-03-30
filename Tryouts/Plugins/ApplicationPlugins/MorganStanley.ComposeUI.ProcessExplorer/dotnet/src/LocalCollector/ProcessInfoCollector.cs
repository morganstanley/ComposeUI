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
using LocalCollector.Communicator;
using LocalCollector.Connections;
using LocalCollector.EnvironmentVariables;
using LocalCollector.Logging;
using LocalCollector.Modules;
using LocalCollector.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalCollector;

public class ProcessInfoCollector : IProcessInfoCollector
{
    public ProcessInfoCollectorData Data { get; } = new();
    private ICommunicator? _communicator;
    private readonly ILogger<ProcessInfoCollector> _logger;
    private readonly AssemblyInformation _assemblyID = new();
    private readonly object _locker = new();

    ProcessInfoCollector(ICommunicator? channel = null, ILogger<ProcessInfoCollector>? logger = null,
        string? assemblyId = null, int? pid = null)
    {
        this._communicator = channel;
        this._logger = logger ?? NullLogger<ProcessInfoCollector>.Instance;

        if (assemblyId != null)
        {
            this._assemblyID.Name = assemblyId;
        }

        if (pid != null)
        {
            Data.Id = Convert.ToInt32(pid);
        }
    }

    public ProcessInfoCollector(EnvironmentMonitorInfo envs, IConnectionMonitor cons, ICommunicator? channel = null,
        ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
        : this(channel, logger, assemblyId, pid)
    {
        Data.Id = Process.GetCurrentProcess().Id;
        Data.EnvironmentVariables = envs.EnvironmentVariables;
        Data.Connections = cons.Data.Connections;

        SetConnectionChangedEvent(cons);
    }

    public ProcessInfoCollector(EnvironmentMonitorInfo envs, IConnectionMonitor cons,
        RegistrationMonitorInfo registrations, ModuleMonitorInfo modules, ICommunicator? channel = null,
        ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
        : this(envs, cons, channel, logger, assemblyId, pid)
    {
        Data.Registrations = registrations.Services;
        Data.Modules = modules.CurrentModules;
    }

    public ProcessInfoCollector(IConnectionMonitor cons, ICollection<RegistrationInfo> regs,
        ICommunicator? channel = null, ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null,
        int? pid = null)
        : this(EnvironmentMonitorInfo.FromEnvironment(), cons, RegistrationMonitorInfo.FromCollection(regs),
            ModuleMonitorInfo.FromAssembly(), channel, logger, assemblyId, pid)
    {
    }

    public ProcessInfoCollector(IConnectionMonitor cons, IServiceCollection regs, ICommunicator? channel = null,
        ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
        : this(EnvironmentMonitorInfo.FromEnvironment(), cons, RegistrationMonitorInfo.FromCollection(regs),
            ModuleMonitorInfo.FromAssembly(), channel, logger, assemblyId, pid)
    {
    }

    public ProcessInfoCollector(IConnectionMonitor cons, RegistrationMonitorInfo regs, ICommunicator? channel = null,
        ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
        : this(EnvironmentMonitorInfo.FromEnvironment(), cons, regs, ModuleMonitorInfo.FromAssembly(), channel,
            logger, assemblyId, pid)
    {
    }


    private void SetConnectionChangedEvent(IConnectionMonitor connectionMonitor)
    {
        connectionMonitor._connectionStatusChanged += ConnectionStatusChangedHandler;
    }

    public void SetCommunicator(ICommunicator communicator)
    {
        this._communicator = communicator;
        var runtimeInfo = new List<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>>()
        {
            new KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>(_assemblyID, Data)
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
                var info = Data.Connections.FirstOrDefault(p => p.Id == connection.Id);
                if (info != null)
                {
                    var index = Data.Connections.IndexOf(info);
                    if (index >= 0 && Data.Connections.Count <= index)
                    {
                        Data.Connections[Convert.ToInt32(index)] = connection;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.ConnectionStatusChangedError(exception);
        }

        var connections = new List<KeyValuePair<AssemblyInformation, ConnectionInfo>>()
        {
            new(_assemblyID, connection)
        };

        _communicator.UpdateConnectionInformation(connections)
            .ConfigureAwait(true);
    }

    public async Task SendRuntimeInfo()
    {
        if (_communicator != null)
        {
            var runtimeInfo = new List<KeyValuePair<AssemblyInformation, ProcessInfoCollectorData>>()
            {
                new(_assemblyID, Data)
            };

            await _communicator.AddRuntimeInfo(runtimeInfo);
        }
    }

    public async Task AddConnectionMonitor(ConnectionMonitorInfo connections)
    {
        if(_communicator != null)
            await AddOrUpdateElements(
                connections.Connections,
                Data.Connections,
                (item) => (conn) => conn.Id == item.Id,
                _communicator.AddConnectionCollection);
    }


    public async Task AddConnectionMonitor(IConnectionMonitor connections)
    {
        await AddConnectionMonitor(connections.Data);
    }


    public async Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables)
    {
        if (Data.EnvironmentVariables.IsEmpty)
        {
            lock (_locker)
            {
                Data.EnvironmentVariables = environmentVariables.EnvironmentVariables;
            }
        }
        else
        {
            foreach (var env in environmentVariables.EnvironmentVariables)
            {
                lock (_locker)
                {
                    Data.EnvironmentVariables.AddOrUpdate(env.Key, env.Value, (_, _) => env.Value);
                }
            }

        }

        if (_communicator != null)
        {
            var info = new List<KeyValuePair<AssemblyInformation, IEnumerable<KeyValuePair<string, string>>>>()
            {
                new(_assemblyID, environmentVariables.EnvironmentVariables)
            };

            await _communicator.UpdateEnvironmentVariableInformation(info);
        }
    }

    private async Task AddOrUpdateElements<T>(
        SynchronizedCollection<T> source,
        SynchronizedCollection<T> target,
        Func<T, Func<T, bool>> predicate,
        Func<IEnumerable<KeyValuePair<AssemblyInformation, IEnumerable<T>>>, ValueTask> handler)
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
            var info = new List<KeyValuePair<AssemblyInformation, IEnumerable<T>>>()
            {
                new(_assemblyID, source)
            };

            await handler(info);
        }
    }

    public async Task AddRegistrations(RegistrationMonitorInfo registrations)
    {
        if (_communicator != null)
            await AddOrUpdateElements(
                registrations.Services,
                Data.Registrations,
                (item) => (reg) => reg.LifeTime == item.LifeTime && reg.ImplementationType == item.ImplementationType && reg.LifeTime == item.LifeTime,
                _communicator.UpdateRegistrationInformation);
    }

    public async Task AddModules(ModuleMonitorInfo modules)
    {
        if (_communicator != null)
            await AddOrUpdateElements(
                modules.CurrentModules,
                Data.Modules,
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
        this._assemblyID.Name = assemblyId;
    }

    public void SetClientPid(int clientPid)
    {
        Data.Id = clientPid;
    }
}
