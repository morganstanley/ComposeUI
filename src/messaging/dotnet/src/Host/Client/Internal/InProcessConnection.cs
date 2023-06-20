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

using System.Threading.Channels;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Client.Internal;

internal sealed class InProcessConnection : IConnection, IClientConnection
{
    public InProcessConnection(IMessageRouterServer server)
    {
        _server = server;
    }

    ValueTask IClientConnection.SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        return _serverToClient.Writer.WriteAsync(message, cancellationToken);
    }

    ValueTask<Message> IClientConnection.ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _clientToServer.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask CloseAsync()
    {
        _clientToServer.Writer.TryComplete();
        _serverToClient.Writer.TryComplete();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return CloseAsync();
    }

    ValueTask IConnection.ConnectAsync(CancellationToken cancellationToken = default)
    {
        return _server.ClientConnected(this);
    }

    ValueTask IConnection.SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        return _clientToServer.Writer.WriteAsync(message, cancellationToken);
    }

    ValueTask<Message> IConnection.ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _serverToClient.Reader.ReadAsync(cancellationToken);
    }

    private readonly IMessageRouterServer _server;
    private readonly Channel<Message> _clientToServer = Channel.CreateUnbounded<Message>();
    private readonly Channel<Message> _serverToClient = Channel.CreateUnbounded<Message>();
}