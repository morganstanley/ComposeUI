/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Connections;

namespace ProcessExplorer.Entities.Connections
{
    public class ConnectionMonitor : IConnectionMonitor
    {
        public ConnectionMonitorDto Data { get; set; } = new ConnectionMonitorDto();
        private readonly object locker = new object();
        internal Func<ConnectionDto, Task>? SendConnectionStatusChanged;

        public ConnectionMonitor()
        {
            lock(locker)
                Data.Connections = new SynchronizedCollection<ConnectionDto>();
        }

        public ConnectionMonitor(SynchronizedCollection<ConnectionDto> connections)
        {
            lock (locker)
                Data.Connections = connections;
        }

        public void AddConnection(ConnectionDto connectionInfo)
        {
            lock (locker)
            {
                Data.Connections.Add(connectionInfo);
            }
        }

        public void RemoveConnection(ConnectionDto connectionInfo)
        {
            lock (locker)
                Data.Connections.Remove(connectionInfo);
        }

        public void ChangeElement(ConnectionDto connection)
            => AddConnection(connection);

        public ConnectionDto? GetConnection(ConnectionDto connection)
        {
            lock (locker)
            {
                return Data.Connections.Where(conn => conn.Equals(connection)).FirstOrDefault();
            }
        }

        public void StatusChanged(ConnectionDto conn)
        {
            if (Data.Connections.Count > 0)
            {
                int i = 0;
                lock (locker)
                {
                    foreach (var connection in Data.Connections)
                    {
                        if (connection.Id == conn.Id)
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

        public void SetSendConnectionStatusChanged(Func<ConnectionDto, Task> action)
        {
            lock(locker)
                SendConnectionStatusChanged = action;
        }
    }
}
