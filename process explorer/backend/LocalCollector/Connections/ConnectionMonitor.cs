/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector.Connections.Interfaces;

namespace ProcessExplorer.LocalCollector.Connections
{
    public class ConnectionMonitor : IConnectionMonitor
    {
        internal ConnectionMonitorInfo Data { get; set; } = new ConnectionMonitorInfo();
        ConnectionMonitorInfo IConnectionMonitor.Data
        {
            get => this.Data;
            set => this.Data = value;
        }

        private readonly object locker = new object();
        internal Func<ConnectionInfo, Task>? SendConnectionStatusChanged;

        public ConnectionMonitor()
        {
            Data.Connections = new SynchronizedCollection<ConnectionInfo>();
        }

        public ConnectionMonitor(SynchronizedCollection<ConnectionInfo> connections)
        {
            Data.Connections = connections;
        }

        public void AddConnection(ConnectionInfo connectionInfo)
        {
            lock (locker)
            {
                Data.Connections.Add(connectionInfo);
            }
        }

        public void RemoveConnection(ConnectionInfo connectionInfo)
        {
            lock (locker)
                Data.Connections.Remove(connectionInfo);
        }

        public void ChangeElement(ConnectionInfo connection)
            => AddConnection(connection);

        public ConnectionInfo? GetConnection(ConnectionInfo connection)
        {
            lock (locker)
            {
                return Data.Connections.Where(conn => conn.Equals(connection)).FirstOrDefault();
            }
        }

        public void StatusChanged(ConnectionInfo conn)
        {
            if (Data.Connections.Count > 0)
            {
                int i = 0;
                lock (locker)
                {
                    foreach (var connection in Data.Connections)
                    {
                        if (connection.Id == conn.Id && connection.Status != conn.Status)
                        {
                            Data.Connections[i].Status = conn.Status;
                            SendConnectionStatusChanged?.Invoke(conn);
                            break;
                        }
                        i++;
                    }
                }
            }
        }

        public void SetSendConnectionStatusChanged(Func<ConnectionInfo, Task> action)
        {
            lock (locker)
                SendConnectionStatusChanged = action;
        }

        public void AddConnections(SynchronizedCollection<ConnectionInfo> connections)
        {
            lock (locker)
            {
                Data.Connections = connections;
            }
        }

        public void UpdateConnection(ConnectionInfo connectionInfo)
        {
            StatusChanged(connectionInfo);
        }
    }
}
