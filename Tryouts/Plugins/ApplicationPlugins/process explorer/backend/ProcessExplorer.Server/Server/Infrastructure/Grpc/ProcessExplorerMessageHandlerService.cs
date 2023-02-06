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

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Infrastructure.Protos;

namespace ProcessExplorer.Server.Server.Infrastructure.Grpc;

internal class ProcessExplorerMessageHandlerService : ProcessExplorerMessageHandler.ProcessExplorerMessageHandlerBase
{
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly ILogger _logger;

    public ProcessExplorerMessageHandlerService(
        IProcessInfoAggregator processInfoAggregator,
        ILogger? logger = null)
    {
        _processInfoAggregator = processInfoAggregator;
        _logger = logger ?? NullLogger.Instance;
    }

    public override Task Subscribe(Empty request, IServerStreamWriter<Message> responseStream, ServerCallContext context)
    {
        var id = Guid.NewGuid();
        _logger.GrpcClientSubscribedDebug(id.ToString());

        //we will pass the IStreamWriter, which is the stream we can respond to per client
        var handler = new GrpcUIHandler(responseStream, id, _logger);

        try
        {
            _processInfoAggregator.AddUiConnection(handler);

            //wait here until the user is alive
            while (!context.CancellationToken.IsCancellationRequested)
                continue;
        }
        catch (Exception exception)
        {
            _logger.GrpcSubscribeError(id.ToString(), exception, exception);
        }
        finally
        {
            _processInfoAggregator.RemoveUiConnection(handler);
        }

        return Task.CompletedTask;
    }

    public override Task<Empty> Send(Message request, ServerCallContext context)
    {
        //handle here the incoming messages form the clients.
        _logger.GrpcClientMessageReceivedDebug(request.Action.ToString());

        Task.Run(() =>
        {
            MessageHandler.HandleIncomingGrpcMessages(
            request,
            _processInfoAggregator,
            context.CancellationToken);
        });

        return Task.FromResult(new Empty());
    }
}
