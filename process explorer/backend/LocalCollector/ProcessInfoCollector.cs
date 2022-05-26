/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ProcessExplorer.LocalCollector.Communicator;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Logging;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;
using ProcessExplorer.LocalCollector.Connections.Interfaces;

namespace ProcessExplorer.LocalCollector
{
    public class ProcessInfoCollector : IProcessInfoCollector
    {
        #region Properties

        public ProcessInfoCollectorData Data { get; } = new ProcessInfoCollectorData();
        private ICommunicator? channel;
        private readonly ILogger<ProcessInfoCollector>? logger;
        private readonly AssemblyInformation assemblyID = new AssemblyInformation();

        private readonly object locker = new object();

        #endregion

        #region Constructors

        ProcessInfoCollector(ICommunicator? channel = null, ILogger<ProcessInfoCollector>? logger = null,
            string? assemblyId = null, int? pid = null)
        {
            this.channel = channel;
            this.logger = logger;

            if (assemblyId is not null)
            {
                this.assemblyID.Name = assemblyId;
            }

            if (pid is not null)
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

        #endregion

        private void SetConnectionChangedEvent(IConnectionMonitor connectionMonitor)
        {
            connectionMonitor.ConnectionStatusChanged += ConnectionStatusChangedHandler;
        }

        public void SetCommunicator(ICommunicator communicator)
        {
            this.channel = communicator;
            channel.AddRuntimeInfo(assemblyID, Data);
        }


        private void ConnectionStatusChangedHandler(object? sender, ConnectionInfo conn)
        {
            if (channel is not null)
            {
                try
                {
                    lock (locker)
                    {
                        var info = Data.Connections.FirstOrDefault(p => p.Id == conn.Id);
                        if (info is not null)
                        {
                            var index = Data.Connections.IndexOf(info);
                            if (index >= 0 && Data.Connections.Count <= index)
                            {
                                Data.Connections[Convert.ToInt32(index)] = conn;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger?.ConnectionStatusChangedError(exception);
                }

                channel.UpdateConnectionInformation(assemblyID, conn).Wait();
            }
        }

        public async Task SendRuntimeInfo()
        {
            if (channel is not null)
            {
                await channel.AddRuntimeInfo(assemblyID, this.Data);
            }
        }

        public async Task AddConnectionMonitor(ConnectionMonitorInfo connections)
        {
            await AddOrUpdateElements(
                connections.Connections,
                Data.Connections,
                (item) => (conn) => conn.Id == item.Id,
                channel.AddConnectionCollection);
        }


        public async Task AddConnectionMonitor(IConnectionMonitor connections)
        {
            await AddConnectionMonitor(connections.Data);
        }


        public async Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables)
        {
            if (Data.EnvironmentVariables.IsEmpty)
            {
                lock (locker)
                {
                    Data.EnvironmentVariables = environmentVariables.EnvironmentVariables;
                }
            }
            else
            {
                foreach (var env in environmentVariables.EnvironmentVariables)
                {
                    lock (locker)
                    {
                        Data.EnvironmentVariables.AddOrUpdate(env.Key, env.Value, (_, _) => env.Value);
                    }
                }

            }

            if (channel is not null)
            {
                await channel.UpdateEnvironmentVariableInformation(assemblyID, environmentVariables.EnvironmentVariables);
            }
        }

        private async Task AddOrUpdateElements<T>(
            SynchronizedCollection<T> source,
            SynchronizedCollection<T> target,
            Func<T, Func<T, bool>> predicate,
            Func<AssemblyInformation, IEnumerable<T>, Task> handler)
        {
            if (!target.Any())
            {
                lock (locker)
                {
                    target = source;
                }
            }
            else
            {
                foreach (var item in source)
                {
                    lock (locker)
                    {
                        var element = target.FirstOrDefault(predicate(item));
                        var index = target.IndexOf(item);
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
            if(channel is not null)
            {
                await handler(assemblyID, source);
            }
        }

        public async Task AddRegistrations(RegistrationMonitorInfo registrations)
        {
            await AddOrUpdateElements(
                registrations.Services,
                Data.Registrations,
                (item) => (reg) => reg.LifeTime == item.LifeTime && reg.ImplementationType == item.ImplementationType && reg.LifeTime == item.LifeTime,
                channel.UpdateRegistrationInformation);
        }

        public async Task AddModules(ModuleMonitorInfo modules)
        {
            await AddOrUpdateElements(
                modules.CurrentModules,
                Data.Modules,
                (item) => (mod) => mod.Name == item.Name && mod.PublicKeyToken == item.PublicKeyToken && mod.Version == item.Version,
                channel.UpdateModuleInformation);
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

        public void SetAssemblyID(string assemblyID)
        {
            this.assemblyID.Name = assemblyID;
        }

        public void SetClientPID(int clientPID)
        {
            Data.Id = clientPID;
        }
    }
}
