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
using System.Reactive.Subjects;

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;

public sealed class ConnectionInfo : IConnectionInfo
{
    public ConnectionInfo(
        Guid id,
        string name,
        ConnectionStatus status,
        string? localEndpoint = null,
        string? remoteEndpoint = null,
        string? remoteApplication = null,
        string? remoteHostname = null,
        IEnumerable<KeyValuePair<string, string>>? connectionInformation = null)
    {
        Id = id;
        Name = name;
        LocalEndpoint = localEndpoint;
        RemoteEndpoint = remoteEndpoint;
        RemoteApplication = remoteApplication;
        RemoteHostname = remoteHostname;

        if (connectionInformation != null)
            ConnectionInformation = new(connectionInformation);

        Status = status.ToStringCached();
    }

    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? LocalEndpoint { get; private set; }
    public string? RemoteEndpoint { get; private set; }
    public string? RemoteApplication { get; private set; }
    public string? RemoteHostname { get; private set; }
    public ConcurrentDictionary<string, string>? ConnectionInformation { get; init; }
    public string Status { get; private set; }
    protected readonly Subject<KeyValuePair<string, ConnectionStatus>> ConnectionStatusSubject = new();
    public IObservable<KeyValuePair<string, ConnectionStatus>> ConnectionStatusEvents => ConnectionStatusSubject;

    private readonly object _connectionInformationLock = new();

    public void UpdateConnection(
        string? localEndpoint = null,
        string? remoteEndpoint = null,
        string? remoteApplication = null,
        string? remoteHostname = null,
        IEnumerable<KeyValuePair<string, string>>? connectionInformation = null,
        ConnectionStatus status = ConnectionStatus.Unknown)
    {
        if (localEndpoint != null) LocalEndpoint = localEndpoint;
        if (remoteEndpoint != null) RemoteEndpoint = remoteEndpoint;
        if (remoteApplication != null) RemoteApplication = remoteApplication;
        if (remoteHostname != null) RemoteHostname = remoteHostname;
        if (connectionInformation != null && connectionInformation.Any()) AddOrUpdateConnectionInformation(connectionInformation);

        if (status != ConnectionStatus.Unknown)
        {
            Status = status.ToStringCached();
            ConnectionStatusSubject.OnNext(new(Id.ToString(), status));
        }
    }

    private void AddOrUpdateConnectionInformation(IEnumerable<KeyValuePair<string, string>> connectionInformation)
    {
        lock (_connectionInformationLock)
        {
            foreach (var connectionInfo in connectionInformation)
            {
                ConnectionInformation.AddOrUpdate(connectionInfo.Key, connectionInfo.Value,
                    (_, _) => connectionInfo.Value);
            }
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;

        if (obj.GetType() != typeof(ConnectionInfo)) return false;

        return Id == ((ConnectionInfo)obj).Id
               && Name == ((ConnectionInfo)obj).Name;
    }
}
