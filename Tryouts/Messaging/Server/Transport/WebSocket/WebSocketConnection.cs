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

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Messages;
using MorganStanley.ComposeUI.Tryouts.Messaging.Core.Serialization;
using MorganStanley.ComposeUI.Tryouts.Messaging.Server.Transport.Abstractions;

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server.Transport.WebSocket;

internal class WebSocketConnection : IClientConnection
{
    public WebSocketConnection(
        IMessageRouterServer messageRouter,
        ILogger<WebSocketConnection>? logger = null)
    {
        _messageRouter = messageRouter;
        _logger = logger ?? NullLogger<WebSocketConnection>.Instance;
        // This is needed because this weird behavior in System.Threading.Channels: https://github.com/dotnet/runtime/issues/64051
        _stopTokenSource.Token.Register(
            () =>
            {
                _inputChannel.Writer.TryComplete();
                _outputChannel.Writer.TryComplete();
            });
    }

    public ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        return _outputChannel.Writer.WriteAsync(message, cancellationToken);
    }

    public ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _inputChannel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();

        return default;
    }

    public async Task HandleWebSocketRequest(
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("WebSocket client connected");

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token);

        try
        {
            await _messageRouter.ClientConnected(this);

            await Task.WhenAll(
                ReceiveMessagesAsync(webSocket, cts.Token),
                SendMessagesAsync(webSocket, cts.Token));
        }
        finally
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("WebSocket client disconnected");
            }

            await _messageRouter.ClientDisconnected(this);
        }

        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    private readonly Channel<Message> _inputChannel = Channel.CreateUnbounded<Message>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

    private readonly ILogger<WebSocketConnection> _logger;
    private readonly IMessageRouterServer _messageRouter;

    private readonly Channel<Message> _outputChannel = Channel.CreateUnbounded<Message>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private readonly CancellationTokenSource _stopTokenSource = new();

    private async Task ReceiveMessagesAsync(
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        var pipe = new Pipe();

        try
        {
            while (!webSocket.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested)
            {
                var buffer = pipe.Writer.GetMemory(1024 * 4);

                try
                {
                    var receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Close message received from WebSocket client");
                        }

                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

                        break;
                    }

                    pipe.Writer.Advance(receiveResult.Count);

                    if (receiveResult.EndOfMessage)
                    {
                        await pipe.Writer.FlushAsync(CancellationToken.None);
                        var readResult = await pipe.Reader.ReadAsync(CancellationToken.None);
                        var readBuffer = readResult.Buffer;

                        while (!readBuffer.IsEmpty && TryReadMessage(ref readBuffer, out var message))
                        {
                            await _inputChannel.Writer.WriteAsync(message, cancellationToken);
                        }

                        pipe.Reader.AdvanceTo(readBuffer.Start, readBuffer.End);
                    }
                }
                catch (WebSocketException)
                {
                    _logger.LogError("The WebSocket connection dropped unexpectedly");

                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            _inputChannel.Writer.TryComplete(e);
        }
        finally
        {
            _stopTokenSource.Cancel();
        }
    }

    private async Task SendMessagesAsync(
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _outputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (webSocket.State != WebSocketState.Open || cancellationToken.IsCancellationRequested)
                    break;

                var buffer = JsonMessageSerializer.SerializeMessage(message);

                await webSocket.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            _outputChannel.Writer.TryComplete(e);
        }
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out Message? message)
    {
        var innerBuffer = buffer;

        try
        {
            message = JsonMessageSerializer.DeserializeMessage(ref innerBuffer);
            buffer = buffer.Slice(innerBuffer.Start);

            return true;
        }
        catch
        {
            message = null;

            return false;
        }
    }
}
