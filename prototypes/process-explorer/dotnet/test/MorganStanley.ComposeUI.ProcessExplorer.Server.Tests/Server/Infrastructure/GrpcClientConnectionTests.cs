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

using Grpc.Core;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Server.Server.Infrastructure.Grpc;
using ProcessExplorer.Abstractions.Infrastructure.Protos;

namespace MorganStanley.ComposeUI.ProcessExplorer.Server.Tests.Server.Infrastructure;

public class GrpcClientConnectionTests
{
    [Fact]
    public async Task IServerStreamWriter_will_be_triggered()
    {
        var serverStreamWriterMock = new Mock<IServerStreamWriter<Message>>();
        var id = Guid.NewGuid();

        var grpcClient = new GrpcClientConnection(
            responseStream: serverStreamWriterMock.Object,
            id: id);

        var dummyMessage = new Message()
        {
            Action = ActionType.SubscriptionAliveAction,
            Description = "dummy message"
        };

        await grpcClient.SendMessage(dummyMessage);

        serverStreamWriterMock.Verify(x => x.WriteAsync(dummyMessage));
    }
}
