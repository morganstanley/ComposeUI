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

namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;

public interface IConnectionInfo
{
    public Guid Id { get; }
    public string Name { get; }
    public string? LocalEndpoint { get; }
    public string? RemoteEndpoint { get; }
    public string? RemoteApplication { get; }
    public string? RemoteHostname { get; }
    public ConcurrentDictionary<string, string>? ConnectionInformation { get; }
    public string Status { get; }
    IObservable<KeyValuePair<string, ConnectionStatus>> ConnectionStatusEvents { get; }

    void UpdateConnection(
        string? localEndpoint = null,
        string? remoteEndpoint = null,
        string? remoteApplication = null,
        string? remoteHostname = null,
        IEnumerable<KeyValuePair<string, string>>? connectionInformation = null,
        ConnectionStatus status = ConnectionStatus.Unknown);
}
