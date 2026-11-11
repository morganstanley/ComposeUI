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

using System.Net.WebSockets;

namespace MorganStanley.ComposeUI.Messaging.Server.WebSocket;

public class WebSocketConnectionTests
{
    [Fact]
    public async Task A_requested_policy_violation_close_sends_the_close_frame_with_the_reason()
    {
        var webSocket = new FakeWebSocket(WebSocketState.Open);
        var connection = new WebSocketConnection(new Mock<IMessageRouterServer>().Object);
        await connection.CloseWithPolicyViolationAsync("Test reason");

        await connection.HandleWebSocketRequest(webSocket, CancellationToken.None).WaitAsync(TestTimeout);

        webSocket.SentCloseStatus.Should().Be(WebSocketCloseStatus.PolicyViolation);
        webSocket.SentCloseDescription.Should().Be("Test reason");
    }

    [Fact]
    public async Task No_close_frame_is_sent_when_the_socket_is_no_longer_open()
    {
        var webSocket = new FakeWebSocket(WebSocketState.Aborted);
        var connection = new WebSocketConnection(new Mock<IMessageRouterServer>().Object);
        await connection.CloseWithPolicyViolationAsync("Test reason");

        await connection.HandleWebSocketRequest(webSocket, CancellationToken.None).WaitAsync(TestTimeout);

        webSocket.SentCloseStatus.Should().BeNull();
    }

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(2);

    private sealed class FakeWebSocket : System.Net.WebSockets.WebSocket
    {
        public FakeWebSocket(WebSocketState state)
        {
            _state = state;
        }

        public WebSocketCloseStatus? SentCloseStatus { get; private set; }

        public string? SentCloseDescription { get; private set; }

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;

            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            SentCloseStatus = closeStatus;
            SentCloseDescription = statusDescription;
            _state = WebSocketState.CloseSent;
            _closeFrameSent.TrySetResult();

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        // The client answers the close frame of the server with its own.
        public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            await _closeFrameSent.Task.WaitAsync(cancellationToken);
            _state = WebSocketState.Closed;

            return new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true);
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        private readonly TaskCompletionSource _closeFrameSent = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private WebSocketState _state;
    }
}
