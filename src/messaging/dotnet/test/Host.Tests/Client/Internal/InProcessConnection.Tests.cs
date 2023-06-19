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

using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Client.Internal;

public class InProcessConnectionTests
{
    [Fact]
    public async Task Connect_registers_itself_at_the_server()
    {
        var server = new Mock<IMessageRouterServer>();
        var connection = (IConnection)new InProcessConnection(server.Object);

        await connection.ConnectAsync();

        server.Verify(_ => _.ClientConnected(It.IsAny<IClientConnection>()),Times.Once());
    }

    [Fact]
    public async Task The_client_can_send_messages_to_the_server()
    {
        var connection = new InProcessConnection(Mock.Of<IMessageRouterServer>());
        var clientSideConnection = (IConnection) connection;
        var serverSideConnection = (IClientConnection) connection;
        
        await clientSideConnection.ConnectAsync();

        var message = new ConnectRequest();
        
        await clientSideConnection.SendAsync(message);
        var received = await serverSideConnection.ReceiveAsync();

        received.Should().Be(message);
    }

    [Fact]
    public async Task The_server_can_send_messages_to_the_client()
    {
        var connection = new InProcessConnection(Mock.Of<IMessageRouterServer>());
        var clientSideConnection = (IConnection) connection;
        var serverSideConnection = (IClientConnection) connection;

        await clientSideConnection.ConnectAsync();

        var message = new Protocol.Messages.TopicMessage();

        await serverSideConnection.SendAsync(message);
        var received = await clientSideConnection.ReceiveAsync();

        received.Should().Be(message);
    }
}