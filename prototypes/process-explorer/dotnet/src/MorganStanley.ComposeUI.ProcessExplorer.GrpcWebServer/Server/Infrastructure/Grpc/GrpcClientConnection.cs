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
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Infrastructure.Protos;

namespace MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.Server.Infrastructure.Grpc;

internal class GrpcClientConnection : IClientConnection<Message>
{
    private readonly KeyValuePair<Guid, IServerStreamWriter<Message>> _stream;
    private readonly object _streamLock = new();

    public GrpcClientConnection(
        IServerStreamWriter<Message> responseStream,
        Guid id)
    {
        _stream = new(id, responseStream);
    }

    public Task SendMessage(Message message)
    {
        lock (_streamLock)
        {
            return _stream.Value.WriteAsync(message);
        }
    }
}

