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


namespace MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities.Connections;

public interface IConnectionMonitor
{
    /// <summary>
    /// Contains the information of the relevant connections.
    /// </summary>
    ConnectionMonitorInfo Connections { get; }

    /// <summary>
    /// Event for status change of a connection.
    /// </summary>
    event EventHandler<ConnectionInfo>? _connectionStatusChanged;

    /// <summary>
    /// Adds a connection to the collection.
    /// </summary>
    /// <param name="connectionInfo"></param>
    void AddConnection(ConnectionInfo connectionInfo);

    /// <summary>
    /// Adds elements or updates the elements of the collection.
    /// </summary>
    /// <param name="connections"></param>
    void AddConnections(SynchronizedCollection<ConnectionInfo> connections);

    /// <summary>
    /// Updates a connection in the collection.
    /// </summary>
    /// <param name="connId"></param>
    /// <param name="status"></param>
    void UpdateConnection(Guid connId, ConnectionStatus status);

    /// <summary>
    /// Removes a connection from the list.
    /// </summary>
    /// <param name="connectionInfo"></param>
    void RemoveConnection(ConnectionInfo connectionInfo);
}
