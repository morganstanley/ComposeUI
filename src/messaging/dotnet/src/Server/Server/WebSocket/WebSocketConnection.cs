﻿// Morgan Stanley makes this available to you under the Apache License,
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
using MorganStanley.ComposeUI.Messaging.Exceptions;
using MorganStanley.ComposeUI.Messaging.Protocol.Json;
using MorganStanley.ComposeUI.Messaging.Protocol.Messages;
using MorganStanley.ComposeUI.Messaging.Server.Abstractions;

namespace MorganStanley.ComposeUI.Messaging.Server.WebSocket;

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
                _receiveChannel.Writer.TryComplete();
                _sendChannel.Writer.TryComplete();
            });
    }

    public ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            return _sendChannel.Writer.WriteAsync(message, cancellationToken);
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

    public ValueTask CloseAsync()
    {
        _stopTokenSource.Cancel();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return CloseAsync();
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

    private readonly Channel<Message> _receiveChannel = Channel.CreateUnbounded<Message>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

    private readonly ILogger<WebSocketConnection> _logger;
    private readonly IMessageRouterServer _messageRouter;

    private readonly Channel<Message> _sendChannel = Channel.CreateUnbounded<Message>(
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

                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

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
                            await _receiveChannel.Writer.WriteAsync(message, cancellationToken);
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

            _receiveChannel.Writer.TryComplete(ThrowHelper.ConnectionClosed());
        }
        catch (Exception e)
        {
            _receiveChannel.Writer.TryComplete(ThrowHelper.ConnectionAborted(e));
        }

        _stopTokenSource.Cancel();
    }

    private async Task SendMessagesAsync(
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (webSocket.State != WebSocketState.Open || cancellationToken.IsCancellationRequested)
                    break;

                using var bufferWriter = new ArrayPoolBufferWriter<byte>(ArrayPool<byte>.Shared);
                JsonMessageSerializer.SerializeMessage(message, bufferWriter);

                await webSocket.SendAsync(
                    bufferWriter.WrittenMemory,
                    WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException e) { }
        catch (WebSocketException e) when (e is { WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely }) { }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception while sending messages using the WebSocket: {ExceptionMessage}", e.Message);
        }
        
        _stopTokenSource.Cancel();
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
