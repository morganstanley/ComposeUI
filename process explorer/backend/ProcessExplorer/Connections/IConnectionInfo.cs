/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;

namespace ProcessExplorer.Entities.Connections
{
    public enum ConnectionStatus
    {
        Running,
        Stopped,
        Failed
    }

    public interface IConnection 
    {
        public Guid? Id { get; init; }
        public string? Name { get; set; }
        public string? LocalEndpoint { get;  set; }
        public string? RemoteEndpoint { get;  set; }
        public string? RemoteApplication { get;  set; }
        public string? RemoteHostname { get;  set; }
        public ConcurrentDictionary<string, string>? ConnectionInformation { get; set; }
        public ConnectionStatus GetConnectionStatus(object clientStatus);
    }
    public class DummyConnectionInfo : IConnection
    {
        public DummyConnectionInfo(ref ClientWebSocket client, Uri remote)
        {
            Id = Guid.NewGuid();
            RemoteEndpoint = remote.AbsolutePath; 
            RemoteApplication = remote.IdnHost;
            RemoteHostname = remote.Host;
            LocalEndpoint = Assembly.GetEntryAssembly()?.Location;
            Status = GetConnectionStatus(client.State);
        }
        public DummyConnectionInfo(Guid id, ConnectionStatus status, string name, string local,string remoteendpoint, 
            string remoteApplication, string remotehost, ConcurrentDictionary<string, string>? connectionInformation)
        {
            this.Id = id;
            this.Status = status;
            this.Name = name;
            this.LocalEndpoint = local;
            this.RemoteEndpoint = remoteendpoint;
            this.RemoteHostname = remotehost;
            this.ConnectionInformation = connectionInformation;
            this.RemoteApplication = remoteApplication;
        }

        private ConnectionStatus status;
        public ConnectionStatus Status { 
            get
                => status; 
            set {
                SetField(ref status, value);
            } }
        public string? Name { get; set; }
        public string? LocalEndpoint { get; set; }
        public string? RemoteEndpoint { get; set; }
        public string? RemoteApplication { get; set; }
        public string? RemoteHostname { get; set; }
        public ConcurrentDictionary<string, string>? ConnectionInformation { get; set; }
        public Guid? Id { get; init;  }

        private bool SetField<T>(ref T status, T value)
        {
            if(EqualityComparer<T>.Default.Equals(value, status)) return false;
            status = value;
            return true;
        }
        public ConnectionStatus GetConnectionStatus(object clientStatus)
        {
            var clientState = (WebSocketState)clientStatus;

            if (clientState == WebSocketState.Open)
                return ConnectionStatus.Running;
            else if (clientState == WebSocketState.Closed || clientState == WebSocketState.None
                 || clientState == WebSocketState.CloseSent)
                return ConnectionStatus.Stopped;
            else if (clientState == WebSocketState.Aborted || clientState == WebSocketState.CloseReceived)
                return ConnectionStatus.Failed;
            return ConnectionStatus.Failed;
        }
    }
}
