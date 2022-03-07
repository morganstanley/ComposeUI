/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector;
using LocalCollector.Connections;
using LocalCollector.Modules;
using ProcessExplorer.Entities.Connections;
using ProcessExplorer.Entities.EnvironmentVariables;
using ProcessExplorer.Entities.Registrations;
using ProcessExplorer.Processes.RPCCommunicator;

namespace ProcessExplorer
{
    public class InfoAggregator : IInfoAggregator
    {
        #region Properties
        public InfoAggregatorDto Data { get; set; } = new InfoAggregatorDto();

        //later it will be removed.
        private readonly HttpClient? httpClient = new HttpClient();

        private readonly ICommunicator? channel;
        private readonly object locker = new object();
        #endregion

        #region Constructors
        InfoAggregator(ICommunicator? channel)
        {
            this.channel = channel;
        }
        public InfoAggregator(Guid id, EnvironmentMonitorDto envs,
            ConnectionMonitor cons, ICommunicator? channel = null)
            : this(channel)
        {
            Data.Id = id;
            Data.EnvironmentVariables = envs;
            Data.Connections = cons.Data;
        }
        public InfoAggregator(Guid id, EnvironmentMonitorDto envs, ConnectionMonitor cons,
            RegistrationMonitorDto registrations, ModuleMonitorDto modules, ICommunicator? channel = null)
            : this(id, envs, cons, channel)
        {
            Data.Registrations = registrations;
            Data.Modules = modules;
        }
        #endregion

        //SAMPLE MESSAGE SENDING
        public async Task SendMessage(object changedElement)
        {
            if (changedElement is not null && channel is not null)
                await channel.SendMessage(changedElement);
        }

        public void AddConnectionMonitor(ConnectionMonitorDto connections)
        {
            lock (locker)
            {
                if (connections.Connections is not null)
                    foreach (var conn in connections.Connections)
                    {
                        if (conn is not null)
                            Data.Connections?.Connections.Add(conn);
                    }
            }
        }
        public void AddConnectionMonitor(ConnectionMonitor connections)
            => AddConnectionMonitor(connections.Data);


        public void AddEnvironmentVariables(EnvironmentMonitorDto environmentVariables)
        {
            if (Data.EnvironmentVariables is null ||
                !Data.EnvironmentVariables.EnvironmentVariables.IsEmpty)
                Data.EnvironmentVariables = environmentVariables;
            else
                lock (locker)
                {
                    foreach (var env in environmentVariables.EnvironmentVariables)
                    {
                        Data.EnvironmentVariables.EnvironmentVariables.AddOrUpdate(
                            env.Key, env.Value, (_, _) => env.Value);
                    }
                }
        }

        public void AddRegistrations(RegistrationMonitorDto registrations)
        {
            if (registrations.Services is not null && Data is not null)
                lock (locker)
                {
                    if (Data?.Registrations is null && Data is not null)
                        Data.Registrations = registrations;
                    else
                    {
                        foreach (var reg in registrations.Services)
                        {
                            if (reg is not null && Data is not null)
                                Data.Registrations.Services.Add(reg);
                        }
                    }
                }
        }

        public void AddModules(ModuleMonitorDto modules)
        {
            if(modules.CurrentModules is not null && Data is not null)
                lock (locker)
                {
                    if(Data?.Modules is null && Data is not null)
                        Data.Modules = modules;
                    else
                    {
                        foreach (var module in modules.CurrentModules)
                        {
                            if(module is not null)
                                Data?.Modules.CurrentModules.Add(module);
                        }
                    }
                }
        }

        public void AddInformation(ConnectionMonitor connections, EnvironmentMonitorDto envrionmentVariables,
            RegistrationMonitorDto registrations, ModuleMonitorDto modules)
        {
            AddConnectionMonitor(connections);
            AddEnvironmentVariables(envrionmentVariables);
            AddRegistrations(registrations);
            AddModules(modules);
        }
    }
}
