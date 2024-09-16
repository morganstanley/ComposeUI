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

using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using System.Collections.Concurrent;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels
{
    internal abstract class Channel : IAsyncDisposable
    {
        public string Id { get; }
        protected IMessagingService MessagingService { get; }
        protected abstract string ChannelTypeName { get; }
        private readonly ILogger _logger;
        private readonly ChannelTopics _topics;
        private readonly ConcurrentDictionary<string, IMessageBuffer> _contexts = new ConcurrentDictionary<string, IMessageBuffer>();
        private readonly object _contextsLock = new();
        private IMessageBuffer? _lastContext = null;
        private IAsyncDisposable? _broadcastSubscription;
        private bool _disposed = false;

        protected Channel(string id, IMessagingService messagingService, ILogger logger, ChannelTopics topics)
        {
            Id = id;
            MessagingService = messagingService;
            _logger = logger;
            _topics = topics;
        }

        public async ValueTask Connect()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Channel));
            }

            await MessagingService.ConnectAsync();

            await MessagingService.RegisterServiceAsync(_topics.GetCurrentContext, GetCurrentContext);

            var broadcastHandler = new Func<IMessageBuffer, ValueTask>(HandleBroadcast);
            var broadcastSubscription = MessagingService.SubscribeAsync(_topics.Broadcast, broadcastHandler);

            _broadcastSubscription = await broadcastSubscription;

            LogConnected();
        }

        internal ValueTask HandleBroadcast(IMessageBuffer? payloadBuffer)
        {
            lock (_contextsLock)
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

                _contexts.AddOrUpdate(
                    contextType,
                    _ => payloadBuffer,
                    (_, _) => payloadBuffer);

                _lastContext = payloadBuffer;

                return ValueTask.CompletedTask;
            }
        }

        internal ValueTask<IMessageBuffer?> GetCurrentContext(string endpoint, IMessageBuffer? payloadBuffer, MessageContext? context)
        {
            lock (_contextsLock)
            {
                if (payloadBuffer == null)
                {
                    return ValueTask.FromResult(_lastContext);
                }

                var payload = payloadBuffer.ReadJson<GetCurrentContextRequest>();
                if (payload?.ContextType == null)
                {
                    return ValueTask.FromResult(_lastContext);
                }

                return _contexts.TryGetValue(payload.ContextType, out var messageBuffer)
                    ? ValueTask.FromResult<IMessageBuffer?>(messageBuffer)
                    : ValueTask.FromResult<IMessageBuffer?>(null);
            }
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (_broadcastSubscription != null)
            {
                await _broadcastSubscription.DisposeAsync();
            }

            _broadcastSubscription = null;

            await MessagingService.UnregisterServiceAsync(_topics.GetCurrentContext);

            _disposed = true;
        }

        protected void LogConnected()
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{ChannelTypeName} {Id} connected to MessageRouter with client id {MessagingService.ClientId}");
            }
        }

        protected void LogNullOrEmptyBroadcast()
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning($"{ChannelTypeName} {Id} received a null or empty payload in broadcast. This broadcast will be ignored.");
            }
        }

        protected void LogInvalidPayloadJson()
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning($"{ChannelTypeName} {Id} could not parse the incoming broadcasted payload. This broadcast will be ignored.");
            }
        }

        protected void LogPayload(IMessageBuffer payload)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"{ChannelTypeName} {Id} received broadcasted payload: {payload.GetString()}");
            }
        }

        protected void LogMissingContextType()
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning($"{ChannelTypeName} {Id} received broadcasted payload with no context type specified. This broadcast will be ignored.");
            }
        }
    }
}
