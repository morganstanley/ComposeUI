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
using System.Collections.Concurrent;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;

internal abstract class Channel : IAsyncDisposable
{
    public string Id { get; }
    protected IMessaging MessagingService { get; }
    protected abstract string ChannelTypeName { get; }
    private readonly ILogger _logger;
    private readonly ChannelTopics _topics;
    private readonly ConcurrentDictionary<string, string> _contexts = new();
    private readonly ConcurrentDictionary<string, ContextMetadata?> _contextMetadatas = new();
    private readonly object _contextsLock = new();
    private string? _lastContext = null;
    private ContextMetadata? _lastContextMetadata = null;
    private IAsyncDisposable? _broadcastSubscription;
    private int _disposed = 0;
    protected JsonSerializerOptions JsonSerializerOptions { get; }

    private IAsyncDisposable? _getCurrentContextService;

    protected Channel(string id, IMessaging messagingService, JsonSerializerOptions jsonSerializerOptions, ILogger logger, ChannelTopics topics)
    {
        Id = id;
        MessagingService = messagingService;
        _logger = logger;
        _topics = topics;
        JsonSerializerOptions = jsonSerializerOptions;
    }

    public async ValueTask Connect()
    {
        if (_disposed > 0)
        {
            throw new ObjectDisposedException(nameof(Channel));
        }

        _getCurrentContextService = await MessagingService.RegisterJsonServiceAsync<GetCurrentContextRequest, GetCurrentContextResponse>(_topics.GetCurrentContext, GetCurrentContextWithMetadata, JsonSerializerOptions);

        var broadcastSubscription = MessagingService.SubscribeAsync(_topics.Broadcast, HandleBroadcast);

        _broadcastSubscription = await broadcastSubscription;

        LogConnected();
    }

    internal ValueTask HandleBroadcast(string? payloadBuffer)
    {
        lock (_contextsLock)
        {
            if (payloadBuffer == null)
            {
                LogNullOrEmptyBroadcast();
                return new ValueTask();
            }

            var payload = payloadBuffer;
            if (payload == null || payload.Length == 0)
            {
                LogNullOrEmptyBroadcast();
                return new ValueTask();
            }

            LogPayload(payloadBuffer);

            // FDC3 3.0: Parse BroadcastPayload to extract context and metadata
            BroadcastPayload? broadcastPayload = null;
            string? contextJson = null;
            ContextMetadata? metadata = null;

            try
            {
                broadcastPayload = JsonSerializer.Deserialize<BroadcastPayload>(payload, JsonSerializerOptions);
                if (broadcastPayload?.Context != null)
                {
                    contextJson = broadcastPayload.Context;
                    metadata = broadcastPayload.Metadata;
                }
                else
                {
                    // Fallback: try to parse as raw context (backward compatibility)
                    contextJson = payload;
                }
            }
            catch (JsonException)
            {
                // Fallback: try to parse as raw context (backward compatibility)
                contextJson = payload;
            }

            if (string.IsNullOrEmpty(contextJson))
            {
                LogInvalidPayloadJson();
                return new ValueTask();
            }

            JsonNode ctx;
            try
            {
                ctx = JsonNode.Parse(contextJson, new JsonNodeOptions() { PropertyNameCaseInsensitive = true })!;
            }
            catch (JsonException)
            {
                LogInvalidPayloadJson();
                return new ValueTask();
            }

            var contextType = (string?) ctx!["type"];

            if (string.IsNullOrEmpty(contextType))
            {
                LogMissingContextType();
                return new ValueTask();
            }

            _contexts.AddOrUpdate(
                contextType!,
                _ => contextJson,
                (_, _) => contextJson);

            _contextMetadatas.AddOrUpdate(
                contextType!,
                _ => metadata,
                (_, _) => metadata);

            _lastContext = contextJson;
            _lastContextMetadata = metadata;

            return new ValueTask();
        }
    }

    internal ValueTask<string?> GetCurrentContext(GetCurrentContextRequest? request)
    {
        lock (_contextsLock)
        {
            if (request?.ContextType == null)
            {
                return new ValueTask<string?>(_lastContext);
            }

            return _contexts.TryGetValue(request.ContextType, out var messageBuffer)
                ? new ValueTask<string?>(messageBuffer)
                : new ValueTask<string?>((string?) null);
        }
    }

    // FDC3 3.0: New method that returns context with metadata
    internal ValueTask<GetCurrentContextResponse> GetCurrentContextWithMetadata(GetCurrentContextRequest? request)
    {
        lock (_contextsLock)
        {
            if (request?.ContextType == null)
            {
                return new ValueTask<GetCurrentContextResponse>(new GetCurrentContextResponse
                {
                    Context = _lastContext,
                    Metadata = _lastContextMetadata
                });
            }

            if (_contexts.TryGetValue(request.ContextType, out var messageBuffer))
            {
                _contextMetadatas.TryGetValue(request.ContextType, out var metadata);
                return new ValueTask<GetCurrentContextResponse>(new GetCurrentContextResponse
                {
                    Context = messageBuffer,
                    Metadata = metadata
                });
            }

            return new ValueTask<GetCurrentContextResponse>(new GetCurrentContextResponse
            {
                Context = null,
                Metadata = null
            });
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (Interlocked.Increment(ref _disposed) > 1)
        {
            return;
        }

        var broadcastSubscriptionTask = _broadcastSubscription != null ? _broadcastSubscription.DisposeAsync() : new ValueTask();
        var getCurrentContextTask = _getCurrentContextService != null ? _getCurrentContextService.DisposeAsync() : new ValueTask();

        await broadcastSubscriptionTask.ConfigureAwait(false);
        await getCurrentContextTask.ConfigureAwait(false);
    }

    protected void LogConnected()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("{ChannelTypeName} {Id} connected to the messaging service", ChannelTypeName, Id);
        }
    }

    protected void LogNullOrEmptyBroadcast()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("{ChannelTypeName} {Id} received a null or empty payload in broadcast. This broadcast will be ignored.", ChannelTypeName, Id);
        }
    }

    protected void LogInvalidPayloadJson()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("{ChannelTypeName} {Id} could not parse the incoming broadcasted payload. This broadcast will be ignored.", ChannelTypeName, Id);
        }
    }

    protected void LogPayload(string payload)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("{ChannelTypeName} {Id} received broadcasted payload: {Payload}", ChannelTypeName, Id, payload);
        }
    }

    protected void LogMissingContextType()
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("{ChannelTypeName} {Id} received broadcasted payload with no context type specified. This broadcast will be ignored.", ChannelTypeName, Id);
        }
    }
}