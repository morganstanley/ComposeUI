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
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorganStanley.ComposeUI.Messaging.Client.Abstractions;
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Instrumentation;
using MorganStanley.ComposeUI.Messaging.Protocol.Json;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;

namespace MorganStanley.ComposeUI.Messaging.Client.WebSocket;

internal class WebSocketConnection : IConnection
{
    public WebSocketConnection(
        IOptions<MessageRouterWebSocketOptions> options,
        ILogger<WebSocketConnection>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<WebSocketConnection>.Instance;
    }

    public async ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();

        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                statusDescription: null,
                CancellationToken.None);
        }
    }

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(_options.Value.Uri, cancellationToken);
            _ = Task.Factory.StartNew(ReceiveMessages, TaskCreationOptions.RunContinuationsAsynchronously);
        }
        catch
        {
            _webSocket.Dispose();

            throw;
        }
    }

    public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var bufferWriter = new ArrayPoolBufferWriter<byte>(ArrayPool<byte>.Shared);
            JsonMessageSerializer.SerializeMessage(message, bufferWriter);

            await _webSocket.SendAsync(
                bufferWriter.WrittenMemory,
                WebSocketMessageType.Text,
                WebSocketMessageFlags.EndOfMessage,
                _stopTokenSource.Token);

            OnMessageSent(message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Exception thrown while trying to send a message over the WebSocket: {ExceptionMessage}",
                e.Message);
        }
    }

    public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _receiveChannel.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ChannelClosedException)
        {
            throw ThrowHelper.ConnectionClosed();
        }
        catch (Exception e)
        {
            throw ThrowHelper.ConnectionAborted(e);
        }
    }

    private readonly Channel<Message> _receiveChannel = Channel.CreateUnbounded<Message>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

    private readonly ILogger<WebSocketConnection> _logger;
    private readonly IOptions<MessageRouterWebSocketOptions> _options;
    private readonly CancellationTokenSource _stopTokenSource = new();
    private ClientWebSocket _webSocket = new();

    private async Task ReceiveMessages()
    {
        var pipe = new Pipe();

        try
        {
            while (!_webSocket.CloseStatus.HasValue && !_stopTokenSource.IsCancellationRequested)
            {
                var buffer = pipe.Writer.GetMemory(1024 * 4);

                // The cancellation token is None intentionally, see https://github.com/dotnet/runtime/issues/31566
                var receiveResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    _receiveChannel.Writer.TryComplete();

                    return;
                }

                pipe.Writer.Advance(receiveResult.Count);

                if (receiveResult.EndOfMessage)
                {
                    await pipe.Writer.FlushAsync(CancellationToken.None);
                    var readResult = await pipe.Reader.ReadAsync(CancellationToken.None);
                    var readBuffer = readResult.Buffer;

                    while (!readBuffer.IsEmpty && TryReadMessage(ref readBuffer, out var message))
                    {
                        OnMessageReceived(message);

                        if (!_receiveChannel.Writer.TryWrite(message))
                        {
                            break;
                        }
                    }

                    pipe.Reader.AdvanceTo(readBuffer.Start, readBuffer.End);
                }
            }

            _receiveChannel.Writer.TryComplete();
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Exception thrown while trying to read a message from the WebSocket: {ExceptionMessage}",
                e.Message);
            _receiveChannel.Writer.TryComplete();
        }
    }

    private void OnMessageReceived(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageReceived))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageReceived,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageReceived, message));
        }
    }

    private void OnMessageSent(Message message)
    {
        if (MessageRouterDiagnosticSource.Log.IsEnabled(MessageRouterEventTypes.MessageSent))
        {
            MessageRouterDiagnosticSource.Log.Write(
                MessageRouterEventTypes.MessageSent,
                new MessageRouterEvent(this, MessageRouterEventTypes.MessageSent, message));
        }
    }

    private static bool TryReadMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out Message? message)
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