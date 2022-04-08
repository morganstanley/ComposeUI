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
        public SynchronizedCollection<RegistrationInfo> Registrations { get; set; } = new SynchronizedCollection<RegistrationInfo>();
        public ConcurrentDictionary<string, string> EnvironmentVariables { get; set; } = EnvironmentMonitorInfo.FromEnvironment().EnvironmentVariables;

        public SynchronizedCollection<ConnectionInfo> Connections { get; set; } =
            new SynchronizedCollection<ConnectionInfo>();
        public SynchronizedCollection<ModuleInfo> Modules { get; set; } = ModuleMonitorInfo.FromAssembly().CurrentModules;

        private readonly object locker = new object();

        public void AddOrUpdateConnections(SynchronizedCollection<ConnectionInfo> connections)
        {
            if (connections.Count > 0)
            {
                lock (locker)
                {
                    foreach (var conn in connections)
                    {
                        if (conn is not null)
                        {
                            var element = Connections.FirstOrDefault(c => c.Id == conn.Id);
                            if (element is not null)
                            {
                                var index = Connections.IndexOf(conn);
                                if (index >= 0)
                                {
                                    Connections[index] = conn;
                                }
                                else
                                {
                                    Connections.Add(conn);
                                }
                            }
                            else
                            {
                                Connections.Add(conn);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateConnection(ConnectionInfo connection)
        {
            lock (locker)
            {
                var index = Connections.IndexOf(connection);
                if (index >= 0)
                {
                    Connections[index] = connection;
                }
            }
        }

        public void UpdateEnvironmentVariables(ConcurrentDictionary<string, string> envs)
        {
            EnvironmentVariables = envs;
        }

        public void UpdateRegistrations(SynchronizedCollection<RegistrationInfo> services)
        {
            Registrations = services;
        }

        public void UpdateModules(SynchronizedCollection<ModuleInfo> currentModules)
        {
            Modules = currentModules;

        }
    }
}
