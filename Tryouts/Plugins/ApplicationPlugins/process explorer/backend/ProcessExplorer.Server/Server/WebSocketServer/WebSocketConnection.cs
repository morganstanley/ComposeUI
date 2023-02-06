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
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IO;
using ProcessExplorer.Abstraction;
using ProcessExplorer.Server.Logging;
using ProcessExplorer.Server.Server.Abstractions;
using ProcessExplorer.Server.Server.Infrastructure.WebSocket;

namespace ProcessExplorer.Server.Server.WebSocketServer;

internal class WebSocketConnection : IWebSocketConnection, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IProcessInfoAggregator _processInfoAggregator;
    private readonly CancellationTokenSource _stopTokenSource = new();
    private readonly Channel<WebSocketMessage> _receiveChannel = Channel.CreateUnbounded<WebSocketMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
    private readonly Channel<WebSocketMessage> _sendChannel = Channel.CreateUnbounded<WebSocketMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = false,
    };

    public WebSocketConnection(
        ILogger? logger,
        IProcessInfoAggregator processInfoAggregator)
    {
        _logger = logger ?? NullLogger<WebSocketConnection>.Instance;
        _processInfoAggregator = processInfoAggregator;

        _stopTokenSource.Token.Register(
            () =>
            {
                _receiveChannel.Writer.TryComplete();
                _sendChannel.Writer.TryComplete();
            });
    }

    public ValueTask DisposeAsync()
    {
        _stopTokenSource.Cancel();
        _stopTokenSource.Dispose();

        return ValueTask.CompletedTask;
    }

    public async Task HandleWebSocketRequest(
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        _logger.WebSocketClientSubscribedDebug();

        //here adding the pushing request handlers for process explorer, need to initialize commuunication route for this websocket
        var uiHandler = new WebSocketUIHandler(_logger, this, cancellationToken);
        _processInfoAggregator.AddUiConnection(uiHandler);

        try
        {
            Task.Run(() =>
                MessageHandler.HandleIncomingWebSocketMessages(
                    this,
                    _processInfoAggregator,
                    _stopTokenSource,
                    _stopTokenSource.Token,
                    _logger));

            await Task.WhenAll(
                ReceiveAsync(webSocket, cancellationToken),
                SendAsync(webSocket, cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.WebSocketSubscribeError(exception, exception);
            _processInfoAggregator.RemoveUiConnection(uiHandler);
        }
    }

    public ValueTask<WebSocketMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _receiveChannel.Reader.ReadAsync(cancellationToken);
    }

    private async Task ReceiveAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var pipe = new Pipe();

        try
        {
            while (!webSocket.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested)
            {
                var buffer = pipe.Writer.GetMemory();

                try
                {
                    var receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.WebSocketServerClosedDebug();

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
                catch (WebSocketException exception)
                {
                    _logger.WebSocketServerStoppedError(exception, exception);

                    break;
                }
                catch (OperationCanceledException exception)
                {
                    _logger.WebSocketServerStoppedError(exception, exception);

                    break;
                }
            }

            _receiveChannel.Writer.TryComplete();
        }
        catch (Exception e)
        {
            _receiveChannel.Writer.TryComplete(e);
        }
        finally
        {
            _stopTokenSource.Cancel();
        }
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> readBuffer, out WebSocketMessage message)
    {
        var innerBuffer = readBuffer;

        try
        {
            var reader = new Utf8JsonReader(innerBuffer);

            message = JsonSerializer.Deserialize<WebSocketMessage>(ref reader, _options);

            innerBuffer = innerBuffer.Slice(reader.Position);

            readBuffer = readBuffer.Slice(innerBuffer.Start);

            return true;
        }
        catch (Exception exception)
        {
            _logger.WebSocketMessageReadingError(exception, exception);

            message = null;

            return false;
        }
    }

    private async Task SendAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (webSocket.State != WebSocketState.Open || cancellationToken.IsCancellationRequested)
                    break;

                await using var stream = new RecyclableMemoryStream(MemoryStreamManager);

                message.Serialize(stream);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length),
                    WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            _sendChannel.Writer.TryComplete(e);
        }
    }

    public ValueTask SendAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        return _sendChannel.Writer.WriteAsync(message, cancellationToken);
    }
}
