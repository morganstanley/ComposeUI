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

namespace ProcessExplorer.LocalCollector
{
    public class ProcessInfoCollector : IProcessInfoCollector
    {
        #region Properties

        public ProcessInfoCollectorData Data { get; } = new ProcessInfoCollectorData();
        private ConnectionMonitor ConnectionMonitor { get; }
        private ICommunicator? channel;
        private readonly ILogger<ProcessInfoCollector>? logger;
        private readonly AssemblyInformation assemblyID = new AssemblyInformation();

        private readonly object locker = new object();

        #endregion

        #region Constructors

        ProcessInfoCollector(ConnectionMonitor cons, ICommunicator? channel = null, ILogger<ProcessInfoCollector>? logger = null,
            string? assemblyId = null, int? pid = null)
        {
            this.channel = channel;
            this.logger = logger;
            ConnectionMonitor = cons;
            Data.Connections = ConnectionMonitor.Data.Connections;

            if (assemblyId is not null)
                this.assemblyID.Name = assemblyId;

            if (pid is not null)
                Data.Id = Convert.ToInt32(pid);
        }

        public ProcessInfoCollector(EnvironmentMonitorInfo envs, ConnectionMonitor cons, ICommunicator? channel = null,
            ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
            : this(cons, channel, logger, assemblyId, pid)
        {
            Data.Id = Process.GetCurrentProcess().Id;
            Data.EnvironmentVariables = envs.EnvironmentVariables;
            
            SetConnectionChangedEvent();
        }

        public ProcessInfoCollector(EnvironmentMonitorInfo envs, ConnectionMonitor cons,
            RegistrationMonitorInfo registrations, ModuleMonitorInfo modules, ICommunicator? channel = null,
            ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
            : this(envs, cons, channel, logger, assemblyId, pid)
        {
            Data.Registrations = registrations.Services;
            Data.Modules = modules.CurrentModules;
        }

        public ProcessInfoCollector(ConnectionMonitor cons, ICollection<RegistrationInfo> regs,
            ICommunicator? channel = null, ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null,
            int? pid = null)
            : this(EnvironmentMonitorInfo.FromEnvironment(), cons, RegistrationMonitorInfo.FromCollection(regs),
                ModuleMonitorInfo.FromAssembly(), channel, logger, assemblyId, pid)
        {
        }

        public ProcessInfoCollector(ConnectionMonitor cons, IServiceCollection regs, ICommunicator? channel = null,
            ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
            : this(EnvironmentMonitorInfo.FromEnvironment(), cons, RegistrationMonitorInfo.FromCollection(regs),
                ModuleMonitorInfo.FromAssembly(), channel, logger, assemblyId, pid)
        {
        }

        public ProcessInfoCollector(ConnectionMonitor cons, RegistrationMonitorInfo regs, ICommunicator? channel = null,
            ILogger<ProcessInfoCollector>? logger = null, string? assemblyId = null, int? pid = null)
            : this(EnvironmentMonitorInfo.FromEnvironment(), cons, regs, ModuleMonitorInfo.FromAssembly(), channel,
                logger, assemblyId, pid)
        {
        }

        #endregion

        private void SetConnectionChangedEvent()
        {
            ConnectionMonitor.ConnectionStatusChanged += ConnectionStatusChangedHandler;
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
                lock (locker)
                {
                    try
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
                    catch (Exception exception)
                    {
                        logger?.ConnectionStatusChanged(exception);
                    }
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
            bool flag = true;
            lock (locker)
            {
                foreach (var conn in connections.Connections)
                {
                    if (conn is not null)
                    {
                        Data.Connections?.Add(conn);
                    }
                }
            }

            if (flag && channel is not null)
                await channel.AddConnectionCollection(assemblyID, connections.Connections);
        }


        public async Task AddConnectionMonitor(ConnectionMonitor connections)
            => await AddConnectionMonitor(connections.Data);


        public async Task AddEnvironmentVariables(EnvironmentMonitorInfo environmentVariables)
        {
            if (Data.EnvironmentVariables.IsEmpty)
            {
                Data.EnvironmentVariables = environmentVariables.EnvironmentVariables;
            }
            else
                lock (locker)
                {
                    foreach (var env in environmentVariables.EnvironmentVariables)
                    {
                        Data.EnvironmentVariables.AddOrUpdate(
                            env.Key, env.Value, (_, _) => env.Value);
                    }
                }

            if (channel is not null)
                await channel.UpdateEnvironmentVariableInformation(assemblyID, environmentVariables);
        }

        public async Task AddRegistrations(RegistrationMonitorInfo registrations)
        {
            if (registrations.Services.Count > 0)
                lock (locker)
                {
                    if (Data.Registrations.Count == 0)
                        Data.Registrations = registrations.Services;
                    else
                    {
                        foreach (var reg in registrations.Services)
                        {
                            if (reg is not null)
                                Data.Registrations.Add(reg);
                        }
                    }
                }

            if (channel is not null)
                await channel.UpdateRegistrationInformation(assemblyID, registrations);
        }

        public async Task AddModules(ModuleMonitorInfo modules)
        {
            lock (locker)
            {
                if (Data.Modules.Count == 0)
                    Data.Modules = modules.CurrentModules;
                else
                {
                    foreach (var module in modules.CurrentModules)
                    {
                        if (module is not null)
                            Data.Modules.Add(module);
                    }
                }
            }

            if (channel is not null)
                await channel.UpdateModuleInformation(assemblyID, modules);
        }

        public async Task AddRuntimeInformation(ConnectionMonitor connections,
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
