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

using System.Collections.Concurrent;
using System.Diagnostics;
using LocalCollector.Connections;
using LocalCollector.EnvironmentVariables;
using LocalCollector.Modules;
using LocalCollector.Registrations;

namespace LocalCollector;

[Serializable]
public class ProcessInfoCollectorData
{
    public int Id { get; set; } = Process.GetCurrentProcess().Id;
    public SynchronizedCollection<RegistrationInfo> Registrations { get; set; } = new();
    public ConcurrentDictionary<string, string> EnvironmentVariables { get; set; } = EnvironmentMonitorInfo.FromEnvironment().EnvironmentVariables;
    public SynchronizedCollection<ConnectionInfo> Connections { get; set; } = new();
    public SynchronizedCollection<ModuleInfo> Modules { get; set; } = ModuleMonitorInfo.FromAssembly().CurrentModules;

    private readonly object _locker = new();

    public void AddOrUpdateConnections(IEnumerable<ConnectionInfo> connections)
    {
        UpdateOrAdd(connections, Connections, (item) => c => c.Id == item.Id);
    }

    public void UpdateConnection(ConnectionInfo connection)
    {
        lock (_locker)
        {
            var index = Connections.IndexOf(connection);
            if (index == -1)
            {
                var element = Connections.FirstOrDefault(c => c.Id == connection.Id);
                if(element != null)
                    index = Connections.IndexOf(element);
            }
            if (index >= 0)
            {
                Connections[index] = connection;
            }
        }
    }

    public void UpdateOrAddEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> envs)
    {
        foreach (var item in envs)
        {
            EnvironmentVariables.AddOrUpdate(item.Key, item.Value, (_, _) => item.Value);
        }
    }

    public void UpdateOrAddRegistrations(IEnumerable<RegistrationInfo> services)
    {
        UpdateOrAdd(services, Registrations, (item) => reg => reg.ImplementationType == item.ImplementationType &&
                reg.ServiceType == item.ServiceType && reg.LifeTime == item.LifeTime);
    }

    public void UpdateOrAddModules(IEnumerable<ModuleInfo> currentModules)
    {
        UpdateOrAdd(currentModules, Modules, (item) => (item2) => item.Name == item2.Name && item.PublicKeyToken == item2.PublicKeyToken);
    }

    private void UpdateOrAdd<T>(IEnumerable<T> source, SynchronizedCollection<T> target, Func<T, Func<T, bool>> predicate)
    {
        if (!source.Any())
        {
            return;
        }

        lock (_locker)
        {
            foreach (var item in source)
            {
                var element = target.FirstOrDefault(predicate(item));
                if (element != null)
                {
                    var index = target.IndexOf(element);
                    if (index >= 0)
                    {
                        target[index] = item;
                    }
                    else
                    {
                        target.Add(item);
                    }
                }
                else
                {
                    target.Add(item);
                }
            }
        }
    }
}
