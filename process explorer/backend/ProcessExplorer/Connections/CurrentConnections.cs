/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

namespace ProcessExplorer.Entities.Connections
{
    public class ConnectionMonitor 
    {
        public List<IConnection> connections;
        public ConnectionMonitor()
        {
            connections = new List<IConnection>();
        }
        public void AddConnection(ref IConnection connectionInfo)
            => connections.Add(connectionInfo);
        public void RemoveConnection(ref IConnection connectionInfo)
            => connections.Remove(connectionInfo);
        public void ChangeElement(IConnection connection)
        {
            var element = connections.FindIndex(conn => conn.Id == connection.Id);
            if (element != default)
            {
                connections[element] = connection;
            }
            else
            {
                connections.Add(connection);
            }
        }
        public ConnectionMonitor(List<IConnection> connections)
            => this.connections = connections;
        public IConnection? GetConnection(IConnection connection) 
            => connections.Where(conn => conn.Equals(connection)).FirstOrDefault();

        public List<IConnection>? GetConnections()
            => connections;

        public void StatusChanged(ref IConnection conn)
        {
            throw new NotImplementedException(); //triggering event tbc
        }
    }
}
