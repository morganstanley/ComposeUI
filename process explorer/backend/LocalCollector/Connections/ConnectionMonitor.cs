/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.LocalCollector.Connections.Interfaces;

namespace ProcessExplorer.LocalCollector.Connections
{
    public class ConnectionMonitor : IConnectionMonitor
    {
        internal ConnectionMonitorInfo Data { get; private set; } = new ConnectionMonitorInfo();
        ConnectionMonitorInfo IConnectionMonitor.Data
        {
            get => this.Data;
            set => this.Data = value;
        }

        private readonly object locker = new object();

        public event EventHandler<ConnectionInfo>? ConnectionStatusChanged;

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
            {
                Data.Connections.Remove(connectionInfo);
            }
        }

        public void AddConnections(SynchronizedCollection<ConnectionInfo> connections)
        {
            foreach (var conn in connections)
            {
                lock (locker)
                {
                    var element = Data.Connections.FirstOrDefault(item => item.Id == conn.Id);
                    var index = Data.Connections.IndexOf(conn);
                    if (index != -1)
                    {
                        Data.Connections[index] = conn;
                    }
                    else
                    {
                        Data.Connections.Add(conn);
                    }
                }
            }
        }

        public void UpdateConnection(Guid connId, ConnectionStatus status)
        {
            if (Data.Connections.Count > 0)
            {
                lock (locker)
                {
                    var conn = Data.Connections.FirstOrDefault(c => c.Id == connId);
                    if (conn is not null && conn.Status != status.ToStringCached())
                    {
                        conn.Status = status.ToStringCached();
                        ConnectionStatusChanged?.Invoke(this, conn);
                    }
                }
            }
        }
    }
}
