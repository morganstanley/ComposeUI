/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using ProcessExplorer.Entities.Connections;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;

namespace LocalCollector.Connections
{
    public class DummyConnectionInfo
    {
        DummyConnectionInfo()
        {
            Data = new ConnectionDto();
        }
        public DummyConnectionInfo(ClientWebSocket client, Uri remote)
            : this()
        {
            Data.Id = Guid.NewGuid();
            Data.RemoteEndpoint = remote.AbsolutePath;
            Data.RemoteApplication = remote.IdnHost;
            Data.RemoteHostname = remote.Host;
            Data.LocalEndpoint = Assembly.GetEntryAssembly()?.Location;
            Data.Status = StatusChanged(client.State);
        }
        public DummyConnectionInfo(Guid id, ConnectionStatus status, string name, string local, string remoteendpoint,
            string remoteApplication, string remotehost, ConcurrentDictionary<string, string>? connectionInformation)
            : this()
        {
            this.Data.Id = id;
            this.Data.Status = status.ToStringCached();
            this.Data.Name = name;
            this.Data.LocalEndpoint = local;
            this.Data.RemoteEndpoint = remoteendpoint;
            this.Data.RemoteHostname = remotehost;
            this.Data.ConnectionInformation = connectionInformation;
            this.Data.RemoteApplication = remoteApplication;
        }

        public ConnectionDto Data { get; set; }

        public string StatusChanged(WebSocketState clientStatus)
        {
            var clientState = clientStatus;

            if (clientState == WebSocketState.Open)
                return ConnectionStatus.Running.ToStringCached();
            else if (clientState == WebSocketState.Closed || clientState == WebSocketState.None
                 || clientState == WebSocketState.CloseSent)
                return ConnectionStatus.Stopped.ToStringCached();
            return ConnectionStatus.Failed.ToStringCached();
        }
    }
}
