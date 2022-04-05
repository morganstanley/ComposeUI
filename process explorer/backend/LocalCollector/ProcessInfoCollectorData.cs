/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections.Concurrent;
using System.Diagnostics;
using ProcessExplorer.LocalCollector.Connections;
using ProcessExplorer.LocalCollector.EnvironmentVariables;
using ProcessExplorer.LocalCollector.Modules;
using ProcessExplorer.LocalCollector.Registrations;

namespace ProcessExplorer.LocalCollector
{
    public class ProcessInfoCollectorData
    {
        public int Id { get; set; } = Process.GetCurrentProcess().Id;
        public RegistrationMonitorInfo? Registrations { get; set; }
        public EnvironmentMonitorInfo? EnvironmentVariables { get; set; }
        public ConnectionMonitorInfo? Connections { get; set; }
        public ModuleMonitorInfo? Modules { get; set; }

        public static ProcessInfoCollectorData AddOrUpdateConnections(object locker ,ProcessInfoCollectorData Data, SynchronizedCollection<ConnectionInfo> connections)
        {
            lock (locker)
            {
                foreach (var conn in connections)
                {
                    if (Data.Connections is not null)
                    {
                        var index = Data.Connections.Connections.IndexOf(conn);
                        if (index >= 0)
                        {
                            Data.Connections.Connections.RemoveAt(index);
                        }
                        Data.Connections.Connections.Add(conn);
                    }
                }
            }
            return Data;
        }

        public static ProcessInfoCollectorData UpdateConnection(object locker, ProcessInfoCollectorData Data, ConnectionInfo connection)
        {
            lock (locker)
            {
                if(Data.Connections is not null)
                {
                    var index = Data.Connections.Connections.IndexOf(connection);
                    if(index >= 0)
                    {
                        Data.Connections.Connections.RemoveAt(index);
                        Data.Connections.Connections.Add(connection);
                    }
                }
            }
            return Data;
        }

        public static ProcessInfoCollectorData UpdateEnvironmentVariables(object locker, ProcessInfoCollectorData Data, ConcurrentDictionary<string, string> envs)
        {
            lock (locker)
            {
                if (Data.EnvironmentVariables is not null)
                {
                    Data.EnvironmentVariables.EnvironmentVariables = envs;
                }
            }
            return Data;
        }

        public static ProcessInfoCollectorData UpdateRegistrations(object locker, ProcessInfoCollectorData Data, SynchronizedCollection<RegistrationInfo> services)
        {
            lock (locker)
            {
                if (Data.Registrations is not null)
                {
                    Data.Registrations.Services = services;
                }
            }
            return Data;
        }

        public static ProcessInfoCollectorData UpdateModules(object locker, ProcessInfoCollectorData Data, SynchronizedCollection<ModuleInfo> currentModules)
        {
            lock (locker)
            {
                if (Data.Modules is not null)
                {
                    Data.Modules.CurrentModules = currentModules;
                }
            }
            return Data;
        }
    }
}
