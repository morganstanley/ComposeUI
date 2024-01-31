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

using System.Linq.Expressions;
using System.Threading.Channels;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.TestUtils;

public class MockClientConnection : Mock<IClientConnection>
{
    public MockClientConnection()
    {
        Setup(_ => _.SendAsync(Capture.In(Received), It.IsAny<CancellationToken>()));

        Setup(_ => _.ReceiveAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => await _sendChannel.Reader.ReadAsync(ct));

        Setup(_ => _.CloseAsync())
            .Callback(
                () => { _sendChannel.Writer.TryComplete(); });
    }

    private readonly Channel<Message> _sendChannel = Channel.CreateUnbounded<Message>();

    public List<Message> Received { get; } = new();

    public string? ClientId { get; private set; }

    public async Task Connect()
    {
        var tcs = new TaskCompletionSource<ConnectResponse>();
        
        Handle<ConnectResponse>(msg => tcs.SetResult(msg));

        await SendToServer(new ConnectRequest());
        var response = await tcs.Task;
        ClientId = response.ClientId;
    }

    public void Close(Exception exception)
    {
        _sendChannel.Writer.TryComplete(exception);
    }

    public void Handle<TMessage>(Expression<Func<TMessage, bool>> filter, Func<TMessage, ValueTask> action)
        where TMessage : Message
    {
        Setup(_ => _.SendAsync(It.Is<TMessage>(filter), It.IsAny<CancellationToken>()))
            .Returns((TMessage msg, CancellationToken _) => action(msg));
    }

    public void Handle<TMessage>(Func<TMessage, ValueTask> action) where TMessage : Message
    {
        Setup(_ => _.SendAsync(It.IsAny<TMessage>(), It.IsAny<CancellationToken>()))
            .Returns((TMessage msg, CancellationToken _) => action(msg));
    }

    public void Handle<TMessage>(Action<TMessage> action) where TMessage : Message
    {
        Setup(_ => _.SendAsync(It.IsAny<TMessage>(), It.IsAny<CancellationToken>()))
            .Returns(
                (TMessage msg, CancellationToken _) =>
                {
                    action(msg);
                    return new ValueTask();
                });
    }

    public void Handle<TRequest, TResponse>(Func<TRequest, TResponse> action)
        where TRequest : AbstractRequest<TResponse>
        where TResponse : AbstractResponse
    {
        Setup(_ => _.SendAsync(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (TRequest request, CancellationToken _) =>
                {
                    var response = action(request);
                    await SendToServer(response);
                });
    }

    public void Handle<TRequest, TResponse>()
        where TRequest : AbstractRequest<TResponse>, new()
        where TResponse : AbstractResponse, new()
    {
        Setup(_ => _.SendAsync(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
            .Returns(
                async (TRequest request, CancellationToken _) =>
                {
                    await SendToServer(
                        new TResponse
                        {
                            RequestId = request.RequestId
                        });
                });
    }

    public void Expect<TMessage>(Expression<Func<TMessage, bool>> expectation) where TMessage : Message
    {
        Expect<TMessage>(expectation, Times.AtLeastOnce());
    }

    public void Expect<TMessage>(Expression<Func<TMessage, bool>> expectation, Func<Times> times)
        where TMessage : Message
    {
        Expect<TMessage>(expectation, times());
    }

    public void Expect<TMessage>(Expression<Func<TMessage, bool>> expectation, Times times) where TMessage : Message
    {
        Verify(_ => _.SendAsync(It.Is<TMessage>(expectation), It.IsAny<CancellationToken>()), times);
    }

    public ValueTask SendToServer(Message message)
    {
        return _sendChannel.Writer.WriteAsync(message);
    }
}
