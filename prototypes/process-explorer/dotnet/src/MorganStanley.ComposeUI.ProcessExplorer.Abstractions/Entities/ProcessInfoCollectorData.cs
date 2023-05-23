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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Extensions;

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;

[Serializable]
public class ProcessInfoCollectorData
{
    public int Id { get; set; } = Process.GetCurrentProcess().Id;
    public IEnumerable<RegistrationInfo> Registrations { get; set; } = Enumerable.Empty<RegistrationInfo>();
    public IEnumerable<KeyValuePair<string, string>> EnvironmentVariables { get; set; } = Enumerable.Empty<KeyValuePair<string, string>>();
    public IEnumerable<IConnectionInfo> Connections { get; set; } = Enumerable.Empty<IConnectionInfo>();
    public IEnumerable<ModuleInfo> Modules { get; set; } = Enumerable.Empty<ModuleInfo>();

    private readonly object _registrationsLocker = new();
    private readonly object _environmentVariablesLocker = new();
    private readonly object _connectionsLocker = new();
    private readonly object _modulesLocker = new();

    public void AddOrUpdateConnections(IEnumerable<IConnectionInfo> connections)
    {
        lock (_connectionsLocker)
        {
            UpdateOrAdd(
            connections,
            Connections,
            (item) => c => c.Id == item.Id,
            out var connectionResult);

            Connections = connectionResult;
        }
    }

    public void UpdateConnection(IConnectionInfo connection)
    {
        lock (_connectionsLocker)
        {
            var index = Connections.IndexOf(connection);

            if (index == -1)
            {
                var element = Connections.FirstOrDefault(c => c.Id == connection.Id);
                if (element != null)
                    index = Connections.IndexOf(element);
            }
            if (index >= 0)
            {
                Connections = Connections.Replace(index, connection);
            }
        }
    }

    public void UpdateConnection(Guid connectionId, ConnectionStatus status)
    {
        lock (_connectionsLocker)
        {
            var connectionInfo = Connections.FirstOrDefault(connection => connection.Id == connectionId);
            if (connectionInfo == null) return;
            connectionInfo.UpdateConnection(status: status);
        }
    }

    public void UpdateOrAddEnvironmentVariables(IEnumerable<KeyValuePair<string, string>> envs)
    {
        lock (_environmentVariablesLocker)
        {
            foreach (var item in envs)
            {
                var index = EnvironmentVariables.Select(x => x.Key).IndexOf(item.Key);
                EnvironmentVariables =
                    index == -1
                        ? EnvironmentVariables.Append(item)
                        : EnvironmentVariables.Replace(index, item);
            }
        }
    }

    public void UpdateOrAddRegistrations(IEnumerable<RegistrationInfo> services)
    {
        lock (_registrationsLocker)
        {
            UpdateOrAdd(services,
            Registrations,
            (item) => (item2) => item2.ImplementationType == item.ImplementationType && item2.ServiceType == item.ServiceType,
            out var registrations);

            Registrations = registrations;
        }
    }

    public void UpdateOrAddModules(IEnumerable<ModuleInfo> currentModules)
    {
        lock (_modulesLocker)
        {
            UpdateOrAdd(
            currentModules,
            Modules,
            (item) => (item2) => item.Name == item2.Name && item.Location == item2.Location,
            out var modules);

            Modules = modules;
        }
    }

    private void UpdateOrAdd<T>(
        IEnumerable<T> source,
        IEnumerable<T> target,
        Func<T, Func<T, bool>> predicate,
        out IEnumerable<T> result)
    {
        result = target;

        if (!source.Any()) return;

        foreach (var item in source)
        {
            var element = target.FirstOrDefault(predicate(item));

            if (element != null)
            {
                var index = target.IndexOf(element);

                if (index >= 0)
                {
                    result = result.Replace(index, item);
                }
                else
                {
                    result = result.Append(item);
                }
            }
            else
            {
                result = result.Append(item);
            }
        }

        result = result.ToArray();
    }
}
