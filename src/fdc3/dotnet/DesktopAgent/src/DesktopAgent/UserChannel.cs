/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Messaging;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent;

internal class UserChannel : IAsyncDisposable
{
    public string Id { get; }
    private readonly ConcurrentDictionary<string, MessageBuffer> _contexts = new ConcurrentDictionary<string, MessageBuffer>();
    private MessageBuffer? _lastContext = null;
    private readonly UserChannelTopics _topics;
    private readonly IMessageRouter _messageRouter;
    private IAsyncDisposable? _broadcastSubscription;
    private bool _disposed = false;
    private readonly ILogger _logger;


    public UserChannel(string id, IMessageRouter messageRouter, ILogger<UserChannel>? logger)
    {
        Id = id;
        _topics = Fdc3Topic.UserChannel(id);
        _messageRouter = messageRouter;
        _logger = (ILogger?) logger ?? NullLogger.Instance;
    }

    public async ValueTask Connect()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UserChannel));
        }

        await _messageRouter.ConnectAsync();

        var broadcastObserver = AsyncObserver.Create<TopicMessage>(x => HandleBroadcast(x.Payload));

        var broadcastSubscribing = _messageRouter.SubscribeAsync(_topics.Broadcast, broadcastObserver);

        await _messageRouter.RegisterServiceAsync(_topics.GetCurrentContext, GetCurrentContext);
        _broadcastSubscription = await broadcastSubscribing;

        LogConnected();
    }

    internal ValueTask HandleBroadcast(MessageBuffer? payloadBuffer)
    {
        if (payloadBuffer == null)
        {
            LogNullOrEmptyBroadcast();
            return ValueTask.CompletedTask;
        }

        var payload = payloadBuffer.GetSpan();
        if (payload == null || payload.Length == 0)
        {
            LogNullOrEmptyBroadcast();
            return ValueTask.CompletedTask;
        }
        LogPayload(payloadBuffer);
        JsonNode ctx;
        try
        {
            ctx = JsonNode.Parse(payload, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;
        }
        catch (JsonException)
        {
            LogInvalidPayloadJson();
            return ValueTask.CompletedTask;
        }
        var contextType = (string?) ctx!["type"];

        if (string.IsNullOrEmpty(contextType))
        {
            LogMissingContextType();
            return ValueTask.CompletedTask;
        }

        _contexts[contextType] = payloadBuffer;
        _lastContext = payloadBuffer;

        return ValueTask.CompletedTask;
    }

    internal ValueTask<MessageBuffer?> GetCurrentContext(string endpoint, MessageBuffer? payloadBuffer, MessageContext? messageContext)
    {
        if (payloadBuffer == null)
        {            
            return ValueTask.FromResult<MessageBuffer?>(_lastContext);
        }

        var payload = payloadBuffer.ReadJson<GetCurrentContextRequest>();
        if (payload?.ContextType == null)
        {
            return ValueTask.FromResult<MessageBuffer?>(_lastContext);
        }

        if (_contexts.TryGetValue(payload.ContextType, out MessageBuffer? context))
        {
            return ValueTask.FromResult<MessageBuffer?>(context);
        }

        return ValueTask.FromResult<MessageBuffer?>(null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_broadcastSubscription != null)
        {
            await _broadcastSubscription.DisposeAsync();
        }
        
        _broadcastSubscription = null;

        await _messageRouter.UnregisterServiceAsync(_topics.GetCurrentContext);

        _disposed = true;
    }

    private void LogConnected()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"UserChannel {Id} connected to messagerouter with client id {_messageRouter.ClientId}");
        }
    }

    private void LogNullOrEmptyBroadcast()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} received a null or empty payload in broadcast. This broadcast will be ignored.");
        }
    }

    private void LogInvalidPayloadJson()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} could not parse the incoming broadcasted payload. This broadcast will be ignored.");
        }
    }

    private void LogPayload(MessageBuffer payload)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug($"UserChannel {Id} received broadcasted payload: {payload.GetString()}");
        }
    }

    private void LogMissingContextType()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning($"UserChannel {Id} received broadcasted payload with no context type specified. This broadcast will be ignored.");
        }
    }
}